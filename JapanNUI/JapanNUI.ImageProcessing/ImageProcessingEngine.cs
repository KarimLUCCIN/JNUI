#define ENABLE_DEBUG_DISPLAY
#define ENABLE_ONLY_BORDER_DISTANCE_LABELS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using JapanNUI.ImageProcessing.Shaders;
using Microsoft.Xna.Framework;
using JapanNUI.ImageProcessing.DebugTools;
using System.IO;
using System.Windows.Media;
using JapanNUI.ImageProcessing.SectionsBuilders;
using System.Runtime.InteropServices;

namespace JapanNUI.ImageProcessing
{
    public class ImageProcessingEngine : IDisposable
    {
        private SoraEngineHost Host { get; set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        private Texture2D noise;

        private RenderTarget2D kinectProcessedOutput, grownRegions;
        private Texture2D kinectDepthSource;

        /* to allow ping pong between the two */
        private RenderTarget2D kinectFlies1, kinectFlies2, fliesPlot;
        private const int fliesBaseCount = 150;
        private const int fliesCount = fliesBaseCount * fliesBaseCount;

        /* to detect contour direction on the detected blobs */
        private RenderTarget2D gradDirectionDetect1, gradDirectionDetect2;

        private BordersDetect bordersDetectShader;
        private Flies fliesShader;

        private DebugTexturePresenter bordersDebugPresenter;
        private DebugTexturePresenter bordersGrownDebugPresenter;
        private DebugTexturePresenter fliesDebugPresenter;
        private DebugTexturePresenter fliesPlotPresenter;
        private DebugTexturePresenter blobsPresenter;
        private DebugTexturePresenter squaresPresenter;

        VertexPositionTexture[] fliesArray;

        RasterizerState wireFrameFlies;

        public ImageProcessingEngine(int width, int height)
        {
            Width = width;
            Height = height;

            Host = new SoraEngineHost();

            Host.Device.DeviceLost += new EventHandler<EventArgs>(Device_DeviceLost);

            Device_DeviceLost(this, EventArgs.Empty);

#if(ENABLE_DEBUG_DISPLAY)
            wireFrameFlies = new RasterizerState();
            wireFrameFlies.CullMode = CullMode.None;
            wireFrameFlies.FillMode = FillMode.WireFrame;

            bordersDebugPresenter = new DebugTexturePresenter("Borders", Width >> 1, Height >> 1);
            bordersGrownDebugPresenter = new DebugTexturePresenter("Borders Grown 1", Width >> 1, Height >> 1);
            squaresPresenter = new DebugTexturePresenter("Squares", Width >> 1, Height >> 1);

            fliesDebugPresenter = new DebugTexturePresenter("Flies", fliesBaseCount, fliesBaseCount);

            fliesArray = new VertexPositionTexture[fliesCount * 6];

            for (int i = 0; i < fliesBaseCount; i++)
            {
                for (int j = 0; j < fliesBaseCount; j++)
                {
                    fliesArray[6 * (i * fliesBaseCount + j) + 0] = new VertexPositionTexture() { Position = new Vector3(i / (float)fliesBaseCount, j / (float)fliesBaseCount, 0), TextureCoordinate = new Vector2(-1, -1) };
                    fliesArray[6 * (i * fliesBaseCount + j) + 1] = new VertexPositionTexture() { Position = new Vector3(i / (float)fliesBaseCount, j / (float)fliesBaseCount, 0), TextureCoordinate = new Vector2(1, -1) };
                    fliesArray[6 * (i * fliesBaseCount + j) + 2] = new VertexPositionTexture() { Position = new Vector3(i / (float)fliesBaseCount, j / (float)fliesBaseCount, 0), TextureCoordinate = new Vector2(1, 1) };

                    fliesArray[6 * (i * fliesBaseCount + j) + 3] = new VertexPositionTexture() { Position = new Vector3(i / (float)fliesBaseCount, j / (float)fliesBaseCount, 0), TextureCoordinate = new Vector2(-1, -1) };
                    fliesArray[6 * (i * fliesBaseCount + j) + 4] = new VertexPositionTexture() { Position = new Vector3(i / (float)fliesBaseCount, j / (float)fliesBaseCount, 0), TextureCoordinate = new Vector2(1, 1) };
                    fliesArray[6 * (i * fliesBaseCount + j) + 5] = new VertexPositionTexture() { Position = new Vector3(i / (float)fliesBaseCount, j / (float)fliesBaseCount, 0), TextureCoordinate = new Vector2(-1, 1) };
                }
            }

            fliesPlotPresenter = new DebugTexturePresenter("Flies Plot", 320, 240);
#endif
        }

        MemoryStream noiseData;

        byte[] grownBordersData;

        void Device_DeviceLost(object sender, EventArgs e)
        {
            DisposeTextures();

            bordersDetectShader = new BordersDetect(Host.Device);
            fliesShader = new Flies(Host.Device);

            kinectDepthSource = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Single, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
            kinectProcessedOutput = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Single, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);

            grownRegions = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, Width >> 1, Height >> 1, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);

            gradDirectionDetect1 = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, grownRegions.Width, grownRegions.Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
            gradDirectionDetect2 = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, grownRegions.Width, grownRegions.Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);

            grownBordersData = new byte[grownRegions.Width * grownRegions.Height * 4];

            blobDelimiter = new BlobDelimiter(grownRegions.Height, grownRegions.Width, 4);
            blobsPresenter = new DebugTexturePresenter("Blobs", grownRegions.Width, grownRegions.Height);

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

            if (gradDirectionDetect1 != null)
                gradDirectionDetect1.Dispose();

            if (gradDirectionDetect2 != null)
                gradDirectionDetect2.Dispose();

            if (kinectProcessedOutput != null)
                kinectProcessedOutput.Dispose();

            if (grownRegions != null)
                grownRegions.Dispose();

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

        ContourBuilder contourBuilder = new ContourBuilder();

        BlobDelimiter blobDelimiter;

        public const int MAXIMUM_CONSIDERED_DEPTH = 2000;
        public const int MINIMUM_KINECT_LITERAL_DEPTH = 800;

        public void Process(byte[] kinectDepthDataBytes, float minDepth, float maxDepth)
        {
            lock (sync)
            {
                var device = Host.Device;

                /* reset device state */
                device.BlendState = BlendState.Opaque;
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = RasterizerState.CullNone;

                for (int i = 0; i < 8; i++)
                    device.Textures[i] = null;

                /* set source data */
                kinectDepthSource.SetData<byte>(kinectDepthDataBytes);

                /* full screen pass */
                bordersDetectShader.halfPixel = new Vector2(0.5f / ((float)Width), 0.5f / ((float)Height));

                bordersDetectShader.depthMap = null;
                bordersDetectShader.depthMap = kinectDepthSource;

                /* Detect */
                Host.RenderTargetManager.Push(kinectProcessedOutput);
                {
                    device.Clear(Microsoft.Xna.Framework.Color.Black);

                    bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["Detect"];
                    bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                    Host.Renderer.QuadRenderer.RenderFullScreen();
                }
                Host.RenderTargetManager.Pop();

                /* Downsize */
                bordersDetectShader.depthMap = kinectProcessedOutput;
                bordersDetectShader.halfPixel = new Vector2(0.5f / ((float)Width), 0.5f / ((float)Height));

                minDepth = (minDepth - MINIMUM_KINECT_LITERAL_DEPTH) / MAXIMUM_CONSIDERED_DEPTH;
                maxDepth = (maxDepth - MINIMUM_KINECT_LITERAL_DEPTH) / MAXIMUM_CONSIDERED_DEPTH;

                bordersDetectShader.minimumDepthOffset = minDepth;
                bordersDetectShader.maximumDepth = maxDepth;

                Host.RenderTargetManager.Push(grownRegions);
                {
                    device.Clear(Microsoft.Xna.Framework.Color.Black);

                    bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["Down"];
                    bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                    Host.Renderer.QuadRenderer.RenderFullScreen();
                }
                Host.RenderTargetManager.Pop();

                bordersGrownDebugPresenter.Update(grownRegions, PixelFormats.Bgr32, 4);


                /* get result : regions */
                kinectProcessedOutput.GetData<byte>(kinectDepthDataBytes);

                /* Process regions to get borders directions */
                bordersDetectShader.depthMap = grownRegions;
                bordersDetectShader.halfPixel = new Vector2(0.5f / (float)grownRegions.Width, 0.5f / (float)grownRegions.Height);

                Host.RenderTargetManager.Push(gradDirectionDetect1);
                {
                    device.Clear(Microsoft.Xna.Framework.Color.Black);

                    bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["Grad"];
                    bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                    Host.Renderer.QuadRenderer.RenderFullScreen();
                }
                Host.RenderTargetManager.Pop();

                if (bordersDebugPresenter != null)
                    //bordersDebugPresenter.Update(kinectDepthDataBytes, System.Windows.Media.PixelFormats.Gray32Float, 4);
                    bordersDebugPresenter.Update(gradDirectionDetect1, System.Windows.Media.PixelFormats.Bgr32, 4);

#warning FUCKING SLOW
                //var contours = contourBuilder.Process(kinectDepthDataBytes, 320, 240);
                grownRegions.GetData(grownBordersData);
                ProcessBlobs(grownBordersData);
                squaresPresenter.Update(grownRegions, PixelFormats.Bgr32, 4);
                //grownBorders.GetData(grownBordersData);

                blobsPresenter.Update(grownBordersData, PixelFormats.Bgr32, 4);

#if(!ENABLE_ONLY_BORDER_DISTANCE_LABELS)
                /* init shader */
                fliesShader.bordersHalfPixel = bordersDetectShader.halfPixel;
                fliesShader.fliesHalfPixel = new Vector2(0.5f / ((float)fliesBaseCount), 0.5f / ((float)fliesBaseCount));
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

                        device.Clear(Microsoft.Xna.Framework.Color.Black);
                        device.RasterizerState = wireFrameFlies;

                        fliesShader.CurrentTechnique = fliesShader.Techniques["Plot"];
                        fliesShader.CurrentTechnique.Passes[0].Apply();

                        device.DrawUserPrimitives(PrimitiveType.TriangleList, fliesArray, 0, fliesCount * 2);
                    }
                    Host.RenderTargetManager.Pop();

                    fliesPlotPresenter.Update(fliesPlot, System.Windows.Media.PixelFormats.Bgr32, 4);
                }

                Swap(ref kinectFlies1, ref kinectFlies2);
#endif

                firstPass = false;
            }
        }

        private unsafe int ProcessBlobs(byte[] kinectDepthDataBytes)
        {
            int blobCount = 0;

            fixed (byte* ptr_kinectDepthDataBytes = &kinectDepthDataBytes[0])
            {
                blobCount = blobDelimiter.BuildBlobs(ptr_kinectDepthDataBytes);
            }

            var device = Host.Device;
            Host.RenderTargetManager.Push(grownRegions);
            {
                device.Clear(Microsoft.Xna.Framework.Color.Black);

                bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["SolidFill"];
                bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                Vector2 a, b;

                int maxColor = 1 << 16;

                var oldRasterizer = device.RasterizerState;
                device.RasterizerState = wireFrameFlies;

                var blobs = blobDelimiter.getBlobs();

                for (int i = 0; i < blobCount; i++)
                {
                    a.X = 2 * ((float)blobs[i].MinX / (float)grownRegions.Width) - 1;
                    a.Y = (2 * ((float)blobs[i].MinY / (float)grownRegions.Height) - 1) * (-1);
                    b.X = 2 * ((float)blobs[i].MaxX / (float)grownRegions.Width) - 1;
                    b.Y = (2 * ((float)blobs[i].MaxY / (float)grownRegions.Height) - 1) * (-1);

                    var d = Vector2.Distance(a, b);
                    if (d > 0.25f)
                    {
                        int currentColor = (maxColor / (blobCount + 1)) * (i+1);

                        bordersDetectShader.SolidFillColor = new Vector3((byte)(currentColor), (byte)(currentColor >> 8), (byte)(currentColor >> 16));

                        Host.Renderer.QuadRenderer.Render(ref a, ref b, 0);


                        a.X = 2 * ((float)(blobs[i].AvgCenterX - 1) / (float)grownRegions.Width) - 1;
                        a.Y = (2 * ((float)(blobs[i].AvgCenterY - 1) / (float)grownRegions.Height) - 1) * (-1);
                        b.X = 2 * ((float)(blobs[i].AvgCenterX + 1) / (float)grownRegions.Width) - 1;
                        b.Y = (2 * ((float)(blobs[i].AvgCenterY + 1) / (float)grownRegions.Height) - 1) * (-1);

                        bordersDetectShader.SolidFillColor = new Vector3((byte)1, (byte)0, (byte)0);

                        Host.Renderer.QuadRenderer.Render(ref a, ref b, 0);
                    }
                }

                device.RasterizerState = oldRasterizer;
            }
            Host.RenderTargetManager.Pop();

            return blobCount;
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
