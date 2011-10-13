#define ENABLE_DEBUG_DISPLAY

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using JapanNUI.ImageProcessing.Shaders;
using Microsoft.Xna.Framework;
using JapanNUI.ImageProcessing.DebugTools;
using System.IO;

namespace JapanNUI.ImageProcessing
{
    public class ImageProcessingEngine : IDisposable
    {
        private SoraEngineHost Host { get; set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        private Texture2D noise;

        private RenderTarget2D kinectProcessedOutput;
        private Texture2D kinectDepthSource;

        /* to allow ping pong between the two */
        private RenderTarget2D kinectFlies1, kinectFlies2, fliesPlot;
        private const int fliesBaseCount = 150;
        private const int fliesCount = fliesBaseCount * fliesBaseCount;

        private BordersDetect bordersDetectShader;
        private Flies fliesShader;

        private DebugTexturePresenter bordersDebugPresenter;
        private DebugTexturePresenter fliesDebugPresenter;
        private DebugTexturePresenter fliesPlotPresenter;

        VertexPositionColor[] fliesArray;

        public ImageProcessingEngine(int width, int height)
        {
            Width = width;
            Height = height;

            Host = new SoraEngineHost();

            Host.Device.DeviceLost += new EventHandler<EventArgs>(Device_DeviceLost);

            Device_DeviceLost(this, EventArgs.Empty);

#if(ENABLE_DEBUG_DISPLAY)
            bordersDebugPresenter = new DebugTexturePresenter("Borders", 320, 240);
            fliesDebugPresenter = new DebugTexturePresenter("Flies", fliesBaseCount, fliesBaseCount);

            fliesArray = new VertexPositionColor[fliesCount * 2];

            for (int i = 0; i < fliesBaseCount; i++)
            {
                for (int j = 0; j < fliesBaseCount; j++)
                {
                    fliesArray[2 * (i * fliesBaseCount + j)] = new VertexPositionColor(){Position = new Vector3(i / (float)fliesBaseCount, j / (float)fliesBaseCount, 0)};
                    fliesArray[2 * (i * fliesBaseCount + j) + 1] = new VertexPositionColor() { Position = new Vector3(i / (float)fliesBaseCount, j / (float)fliesBaseCount, 1) };
                }
            }

            fliesPlotPresenter = new DebugTexturePresenter("Flies Plot", 320, 240);
#endif
        }

        MemoryStream noiseData;

        void Device_DeviceLost(object sender, EventArgs e)
        {
            DisposeTextures();

            bordersDetectShader = new BordersDetect(Host.Device);
            fliesShader = new Flies(Host.Device);

            kinectDepthSource = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Single, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
            kinectProcessedOutput = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Single, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);

            kinectFlies1 = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Vector4, fliesBaseCount, fliesBaseCount, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
            kinectFlies2 = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Vector4, fliesBaseCount, fliesBaseCount, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
            fliesPlot = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, 320, 240, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);


            var noiseBmp = Properties.Resources.Noise;

            if(noiseData == null)
            {
                noiseData= new MemoryStream();
                noiseBmp.Save(noiseData, System.Drawing.Imaging.ImageFormat.Png);
            }

            noiseData.Position = 0;
            noise = Texture2D.FromStream(Host.Device, noiseData, noiseBmp.Width, noiseBmp.Height, false);

            firstPass = true;
        }

        private void DisposeTextures()
        {
            if (kinectFlies1 != null)
                kinectFlies1.Dispose();

            if (kinectFlies2 != null)
                kinectFlies2.Dispose();

            if (kinectDepthSource != null)
                kinectDepthSource.Dispose();

            if (kinectProcessedOutput != null)
                kinectProcessedOutput.Dispose();

            if (fliesPlot != null)
                fliesPlot.Dispose();

            if (bordersDetectShader != null)
                bordersDetectShader.Dispose();

            if (fliesShader != null)
                fliesShader.Dispose();

            if (noise != null)
                noise.Dispose();
        }

        private void Swap(ref RenderTarget2D a, ref RenderTarget2D b)
        {
            var c = a;

            a = b;
            b = c;
        }

        object sync = new object();

        Random rd = new System.Random(DateTime.Now.Millisecond);

        bool firstPass = true;

        public void Process(byte[] kinectDepthDataBytes)
        {
            lock (sync)
            {
                var device = Host.Device;

                /* reset device state */
                device.BlendState = BlendState.Opaque;
                device.DepthStencilState = DepthStencilState.None;

                for (int i = 0; i < 8; i++)
                    device.Textures[i] = null;

                /* set source data */
                kinectDepthSource.SetData<byte>(kinectDepthDataBytes);

                /* full screen pass */
                bordersDetectShader.halfPixel = new Vector2(0.5f / ((float)Width), 0.5f / ((float)Height));

                bordersDetectShader.depthMap = null;
                bordersDetectShader.depthMap = kinectDepthSource;

                Host.RenderTargetManager.Push(kinectProcessedOutput);
                {
                    bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                    Host.Renderer.QuadRenderer.RenderFullScreen();
                }
                Host.RenderTargetManager.Pop();

                /* get result : borders */
                kinectProcessedOutput.GetData<byte>(kinectDepthDataBytes);

                if (bordersDebugPresenter != null)
                    bordersDebugPresenter.Update(kinectDepthDataBytes, System.Windows.Media.PixelFormats.Gray32Float, 4);

                /* init shader */
                fliesShader.halfPixel = new Vector2(0.5f / ((float)fliesBaseCount), 0.5f / ((float)fliesBaseCount));
                fliesShader.noiseMap = noise;
                fliesShader.randomNoiseOffset = (float)(rd.NextDouble() + rd.NextDouble() / float.MaxValue);

                if (firstPass)
                {
                    /* initialize the flies */
                    Host.RenderTargetManager.Push(kinectFlies2);
                    {
                        FliesPass("Reset");
                    }
                    Host.RenderTargetManager.Pop();
                }

                /* Update them */
                Host.RenderTargetManager.Push(kinectFlies1);
                {
                    /* init shader */
                    fliesShader.previousPopulationMap = kinectFlies2;
                    fliesShader.bordersMap = kinectProcessedOutput;

                    /* generation grow */
                    FliesPass("Grow");
                }
                Host.RenderTargetManager.Pop();

                if (fliesDebugPresenter != null)
                    fliesDebugPresenter.Update(kinectFlies1, System.Windows.Media.PixelFormats.Rgba128Float, 16);

                if (fliesPlotPresenter != null)
                {
                    /* plot individual flies for debug purpose */
                    Host.RenderTargetManager.Push(fliesPlot);
                    {
                        fliesShader.previousPopulationMap = kinectFlies1;

                        device.Clear(Color.Black);

                        fliesShader.CurrentTechnique = fliesShader.Techniques["Plot"];
                        fliesShader.CurrentTechnique.Passes[0].Apply();

                        device.DrawUserPrimitives(PrimitiveType.LineList, fliesArray, 0, fliesCount);
                    }
                    Host.RenderTargetManager.Pop();

                    fliesPlotPresenter.Update(fliesPlot, System.Windows.Media.PixelFormats.Bgr32, 4);
                }

                Swap(ref kinectFlies1, ref kinectFlies2);

                firstPass = false;
            }
        }

        private void FliesPass(string technique)
        {
            fliesShader.CurrentTechnique = fliesShader.Techniques[technique];
            fliesShader.CurrentTechnique.Passes[0].Apply();

            Host.Renderer.QuadRenderer.RenderFullScreen();
        }

        ~ImageProcessingEngine()
        {
            Dispose(true);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(false);
        }

        bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                DisposeTextures();

                Host.Dispose();
                Host = null;

                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
