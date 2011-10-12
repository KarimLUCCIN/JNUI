using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using JapanNUI.ImageProcessing.Shaders;
using Microsoft.Xna.Framework;

namespace JapanNUI.ImageProcessing
{
    /// <summary>
    /// Require a DirectX 10 Hardware ... may not be required, but not tested
    /// </summary>
    public class ImageProcessingEngine : IDisposable
    {
        private SoraEngineHost Host { get; set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        private RenderTarget2D kinectProcessedOutput;
        private Texture2D kinectDepthSource;

        private BordersDetect bordersDetect;

        public ImageProcessingEngine(int width, int height)
        {
            Width = width;
            Height = height;

            Host = new SoraEngineHost();

            Host.Device.DeviceLost += new EventHandler<EventArgs>(Device_DeviceLost);

            Device_DeviceLost(this, EventArgs.Empty);
        }

        void Device_DeviceLost(object sender, EventArgs e)
        {
            DisposeTextures();

            bordersDetect = new BordersDetect(Host.Device);

            kinectDepthSource = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.HalfSingle, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
            kinectProcessedOutput = Host.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.HalfSingle, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
        }

        private void DisposeTextures()
        {
            if (kinectDepthSource != null)
                kinectDepthSource.Dispose();

            if (kinectProcessedOutput != null)
                kinectProcessedOutput.Dispose();

            if (bordersDetect != null)
                bordersDetect.Dispose();
        }

        object sync = new object();

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
                bordersDetect.halfPixel = new Vector2(0.5f / ((float)Width), 0.5f / ((float)Height));

                bordersDetect.depthMap = null;
                bordersDetect.depthMap = kinectDepthSource;

                Host.RenderTargetManager.Push(kinectProcessedOutput);
                {
                    bordersDetect.CurrentTechnique.Passes[0].Apply();

                    Host.Renderer.QuadRenderer.RenderFullScreen();
                }
                Host.RenderTargetManager.Pop();

                /* get result */
                kinectProcessedOutput.GetData<byte>(kinectDepthDataBytes);
            }
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
