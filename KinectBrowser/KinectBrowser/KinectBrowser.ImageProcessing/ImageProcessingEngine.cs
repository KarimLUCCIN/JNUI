using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using KinectBrowser.ImageProcessing.Shaders;
using Microsoft.Xna.Framework;
using System.IO;
using System.Windows.Media;
using KinectBrowser.ImageProcessing.SectionsBuilders;
using System.Runtime.InteropServices;
using System.Diagnostics;
using KinectBrowser.D3D;

namespace KinectBrowser.ImageProcessing
{
    public class ImageProcessingEngine : IDisposable
    {
        private SoraEngineHost Host { get; set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        private RenderTarget2D kinectProcessedOutput, grownRegions;
        private Texture2D kinectDepthSource;

        /* to detect contour direction on the detected blobs */
        private RenderTarget2D gradDirectionDetect1, gradDirectionDetect2;

        private BordersDetect bordersDetectShader;

        private ManagedBlob[] mainBlobs;

        /// <summary>
        /// Array of MaxMainBlobsCount describing the biggest blobs found in the data.
        /// Empty blobs will have their PixelCount set to 0. Only the MaxMainBlobsCount biggest blobs
        /// will then be considered by the processing engine.
        /// </summary>
        public ManagedBlob[] MainBlobs
        {
            get { return mainBlobs; }
        }

        private int maxMainBlobsCount;

        public int MaxMainBlobsCount
        {
            get { return maxMainBlobsCount; }
        }

        public ImageProcessingEngine(SoraEngineHost host, int width, int height, int maxMainBlobsCount)
        {
            if (maxMainBlobsCount <= 0)
                throw new OutOfMemoryException("maxMainBlobsCount <= 0");

            this.maxMainBlobsCount = maxMainBlobsCount;

            mainBlobs = new ManagedBlob[maxMainBlobsCount];
            for (int i = 0; i < maxMainBlobsCount; i++)
            {
                mainBlobs[i] = new ManagedBlob();
            }

            Width = width;
            Height = height;

            Host = host;

            Host.CurrentEngine.Device.DeviceLost += new EventHandler<EventArgs>(Device_DeviceLost);

            Device_DeviceLost(this, EventArgs.Empty);
        }

        byte[] grownBordersData;
        byte[] gradDirectionDetect1Data;

        void Device_DeviceLost(object sender, EventArgs e)
        {
            DisposeTextures();

            LineBatch.Init(Host.CurrentEngine.Device);

            bordersDetectShader = new BordersDetect(Host.CurrentEngine.Device);

            kinectDepthSource = Host.CurrentEngine.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Single, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
            kinectProcessedOutput = Host.CurrentEngine.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Single, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);

            grownRegions = Host.CurrentEngine.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, Width >> 1, Height >> 1, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);

            gradDirectionDetect1 = Host.CurrentEngine.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, grownRegions.Width, grownRegions.Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
            gradDirectionDetect2 = Host.CurrentEngine.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, grownRegions.Width, grownRegions.Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);

            grownBordersData = new byte[grownRegions.Width * grownRegions.Height * 4];
            gradDirectionDetect1Data = new byte[gradDirectionDetect1.Width * gradDirectionDetect1.Height * 4];

            blobDelimiter = new BlobDelimiter(grownRegions.Height, grownRegions.Width, 4);
        }

        private void DisposeTextures()
        {
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

            if (bordersDetectShader != null)
                bordersDetectShader.Dispose();
        }

        private void Swap(ref RenderTarget2D a, ref RenderTarget2D b)
        {
            var c = a;

            a = b;
            b = c;
        }

        object sync = new object();

        Random rd = new System.Random(DateTime.Now.Millisecond);

        BlobDelimiter blobDelimiter;

        public const int MAXIMUM_CONSIDERED_DEPTH = 2000;
        public const int MINIMUM_KINECT_LITERAL_DEPTH = 800;

        TimeSpan processingTime = TimeSpan.Zero;
        Stopwatch processingTimeWatch = new Stopwatch();

        public TimeSpan ProcessingTime
        {
            get { return processingTime; }
        }

        public void Process(byte[] kinectDepthDataBytes, float minDepth, float maxDepth)
        {
            processingTimeWatch.Reset();
            processingTimeWatch.Start();
            try
            {
                lock (sync)
                {
                    var device = Host.CurrentEngine.Device;

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
                    Host.CurrentEngine.RenderTargetManager.Push(kinectProcessedOutput);
                    {
                        device.Clear(Microsoft.Xna.Framework.Color.Black);

                        bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["Detect"];
                        bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                        Host.CurrentEngine.Renderer.QuadRenderer.RenderFullScreen();
                    }
                    Host.CurrentEngine.RenderTargetManager.Pop();

                    /* Downsize */
                    bordersDetectShader.depthMap = kinectProcessedOutput;
                    bordersDetectShader.halfPixel = new Vector2(0.5f / ((float)Width), 0.5f / ((float)Height));

                    minDepth = (minDepth - MINIMUM_KINECT_LITERAL_DEPTH) / MAXIMUM_CONSIDERED_DEPTH;
                    maxDepth = (maxDepth - MINIMUM_KINECT_LITERAL_DEPTH) / MAXIMUM_CONSIDERED_DEPTH;

                    bordersDetectShader.minimumDepthOffset = minDepth;
                    bordersDetectShader.maximumDepth = maxDepth;

                    Host.CurrentEngine.RenderTargetManager.Push(grownRegions);
                    {
                        device.Clear(Microsoft.Xna.Framework.Color.Black);

                        bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["Down"];
                        bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                        Host.CurrentEngine.Renderer.QuadRenderer.RenderFullScreen();
                    }
                    Host.CurrentEngine.RenderTargetManager.Pop();

                    //bordersGrownDebugPresenter.Update(grownRegions, PixelFormats.Bgr32, 4);


                    /* get result : regions */
                    //kinectProcessedOutput.GetData<byte>(kinectDepthDataBytes);

                    /* Process regions to get borders directions */
                    bordersDetectShader.depthMap = grownRegions;
                    bordersDetectShader.halfPixel = new Vector2(0.5f / (float)grownRegions.Width, 0.5f / (float)grownRegions.Height);

                    Host.CurrentEngine.RenderTargetManager.Push(gradDirectionDetect1);
                    {
                        device.Clear(Microsoft.Xna.Framework.Color.Black);

                        bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["Grad"];
                        bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                        Host.CurrentEngine.Renderer.QuadRenderer.RenderFullScreen();
                    }
                    Host.CurrentEngine.RenderTargetManager.Pop();

                    gradDirectionDetect1.GetData(gradDirectionDetect1Data);

                    //if (bordersDebugPresenter != null)
                    //    //bordersDebugPresenter.Update(kinectDepthDataBytes, System.Windows.Media.PixelFormats.Gray32Float, 4);
                    //    bordersDebugPresenter.Update(gradDirectionDetect1Data, System.Windows.Media.PixelFormats.Bgr32, 4);

                    //#warning FUCKING SLOW
                    //var contours = contourBuilder.Process(kinectDepthDataBytes, 320, 240);
                    grownRegions.GetData(grownBordersData);
                    ProcessBlobs(grownBordersData, gradDirectionDetect1Data);

                    //grownBorders.GetData(grownBordersData);

                    //blobsPresenter.Update(grownBordersData, PixelFormats.Bgr32, 4);
                }
            }
            finally
            {
                processingTimeWatch.Stop();
                processingTime = processingTimeWatch.Elapsed;
            }
        }

        private List<ManagedBlob> sortingList = new List<ManagedBlob>();

        private unsafe int ProcessBlobs(byte[] mask, byte[] grad)
        {
            int blobCount = 0;

            fixed (byte* ptr_mask = &mask[0])
            {
                fixed (byte* ptr_grad = &grad[0])
                {
                    blobCount = blobDelimiter.BuildBlobs(ptr_mask, ptr_grad);
                }
            }

            sortingList.Clear();
            for (int i = 0; i < blobDelimiter.BlobsValidCount; i++)
            {
                sortingList.Add(blobDelimiter.Blobs[i]);
            }

            sortingList.Sort((a, b) => (-1) * a.PixelCount.CompareTo(b.PixelCount));

            for (int i = 0; i < maxMainBlobsCount && i < sortingList.Count; i++)
            {
                mainBlobs[i] = sortingList[i];
            }

            for (int i = sortingList.Count; i < maxMainBlobsCount; i++)
            {
                mainBlobs[i] = null;
            }

            return blobCount;
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

                Host = null;

                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
