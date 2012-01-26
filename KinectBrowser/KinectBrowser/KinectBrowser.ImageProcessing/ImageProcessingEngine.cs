//#define DOWN_SAMPLE

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
#if(DOWN_SAMPLE)
        public static int DataWidth = 320 >> 1;
        public static int DataHeight = 240 >> 1;
#else
        public static int DataWidth = 320;
        public static int DataHeight = 240;
#endif
        private SoraEngineHost Host { get; set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public RenderTarget2D kinectProcessedOutput;
        public RenderTarget2D grownRegions;
        public Texture2D kinectDepthSource;

        /* to detect contour direction on the detected blobs */
        public RenderTarget2D gradDirectionDetect1, gradDirectionDetect2;

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

        private RasterizerState wireFrameRasterizerState;

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

            wireFrameRasterizerState = new RasterizerState();
            wireFrameRasterizerState.CullMode = CullMode.None;
            wireFrameRasterizerState.FillMode = FillMode.WireFrame;
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

#if(DOWN_SAMPLE)
            grownRegions = Host.CurrentEngine.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, Width >> 1, Height >> 1, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
#else
            grownRegions = Host.CurrentEngine.RenderTargetManager.CreateRenderTarget2D(SurfaceFormat.Color, Width, Height, 0, RenderTargetUsage.PreserveContents, DepthFormat.None);
#endif

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

        private void PrintDebugImage(string name, Texture2D texture)
        {
#if(DEBUG)
            var path = ResolveDebugOutputFileName(name);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            }
#endif
        }

        private static string ResolveDebugOutputFileName(string name)
        {
            var path = "H:\\Kinect Debug Images\\" + name + ".png";
            var dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return path;
        }

        bool displayBlobsDefinition = false;

        public void Process(byte[] kinectDepthDataBytes, float minDepth, float maxDepth)
        {
#if(DEBUG)
            bool printDebugOutput = Host.CurrentEngine.InputManager.IsNewKeyPress(Microsoft.Xna.Framework.Input.Keys.F12);

            if (Host.CurrentEngine.InputManager.IsNewKeyPress(Microsoft.Xna.Framework.Input.Keys.F10))
                displayBlobsDefinition = !displayBlobsDefinition;

            bool drawDebugOutput = printDebugOutput || displayBlobsDefinition;
#endif

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

                    if (printDebugOutput)
                        PrintDebugImage("original-0", kinectDepthSource);

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

                    if (printDebugOutput)
                        PrintDebugImage("detect-1", kinectProcessedOutput);

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

                    if (printDebugOutput)
                        PrintDebugImage("regions-2", grownRegions);

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

                    if (printDebugOutput)
                        PrintDebugImage("grad-3", gradDirectionDetect1);

                    gradDirectionDetect1.GetData(gradDirectionDetect1Data);

                    //if (bordersDebugPresenter != null)
                    //    //bordersDebugPresenter.Update(kinectDepthDataBytes, System.Windows.Media.PixelFormats.Gray32Float, 4);
                    //    bordersDebugPresenter.Update(gradDirectionDetect1Data, System.Windows.Media.PixelFormats.Bgr32, 4);

                    //#warning FUCKING SLOW
                    //var contours = contourBuilder.Process(kinectDepthDataBytes, 320, 240);
                    grownRegions.GetData(grownBordersData);
                    ProcessBlobs(printDebugOutput || drawDebugOutput, grownBordersData, gradDirectionDetect1Data);

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

        private unsafe int ProcessBlobs(bool printDebugOutput, byte[] mask, byte[] grad)
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

            if (printDebugOutput)
            {
                var device = Host.Device;
                Host.CurrentEngine.RenderTargetManager.Push(grownRegions);
                {
                    device.Clear(Microsoft.Xna.Framework.Color.Black);

                    Host.CurrentEngine.Renderer.RendererSpriteBatch.Begin();
                    Host.CurrentEngine.Renderer.RendererSpriteBatch.Draw(gradDirectionDetect1, new Vector2(0, 0), Microsoft.Xna.Framework.Color.White);
                    Host.CurrentEngine.Renderer.RendererSpriteBatch.End();

                    device.BlendState = BlendState.Opaque;
                    device.RasterizerState = wireFrameRasterizerState;
                    bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["SolidFill"];
                    bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                    Vector2 a, b;

                    int maxColor = 1 << 16;

                    var oldRasterizer = device.RasterizerState;
                    device.RasterizerState = wireFrameRasterizerState;

                    var blobs = blobDelimiter.Blobs;

                    for (int i = 0; i < blobCount; i++)
                    {
                        a.X = 2 * ((float)blobs[i].MinX / (float)grownRegions.Width) - 1;
                        a.Y = (2 * ((float)blobs[i].MinY / (float)grownRegions.Height) - 1) * (-1);
                        b.X = 2 * ((float)blobs[i].MaxX / (float)grownRegions.Width) - 1;
                        b.Y = (2 * ((float)blobs[i].MaxY / (float)grownRegions.Height) - 1) * (-1);

                        var d = Vector2.Distance(a, b);
                        if (d > 0.25f)
                        {
                            device.BlendState = BlendState.Opaque;
                            device.RasterizerState = wireFrameRasterizerState;
                            bordersDetectShader.CurrentTechnique = bordersDetectShader.Techniques["SolidFill"];
                            bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                            int currentColor = (maxColor / (blobCount + 1)) * (i + 1);

                            bordersDetectShader.SolidFillColor = new Vector3(1, 0, 0) * (float)(1 - blobs[i].AverageDepth);
                            bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                            Host.CurrentEngine.Renderer.QuadRenderer.Render(ref a, ref b, 0);


                            a.X = 2 * ((float)(blobs[i].AvgCenterX - 1) / (float)grownRegions.Width) - 1;
                            a.Y = (2 * ((float)(blobs[i].AvgCenterY - 1) / (float)grownRegions.Height) - 1) * (-1);
                            b.X = 2 * ((float)(blobs[i].AvgCenterX + 1) / (float)grownRegions.Width) - 1;
                            b.Y = (2 * ((float)(blobs[i].AvgCenterY + 1) / (float)grownRegions.Height) - 1) * (-1);

                            bordersDetectShader.SolidFillColor = new Vector3((byte)1, (byte)0, (byte)0);
                            bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                            Host.CurrentEngine.Renderer.QuadRenderer.Render(ref a, ref b, 0);


                            a.X = 2 * ((float)(blobs[i].EstimatedCursorX - 1) / (float)grownRegions.Width) - 1;
                            a.Y = (2 * ((float)(blobs[i].EstimatedCursorY - 1) / (float)grownRegions.Height) - 1) * (-1);
                            b.X = 2 * ((float)(blobs[i].EstimatedCursorX + 1) / (float)grownRegions.Width) - 1;
                            b.Y = (2 * ((float)(blobs[i].EstimatedCursorY + 1) / (float)grownRegions.Height) - 1) * (-1);

                            bordersDetectShader.SolidFillColor = new Vector3((byte)1, (byte)1, (byte)0);
                            bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                            Host.CurrentEngine.Renderer.QuadRenderer.Render(ref a, ref b, 0);

                            a.X = 2 * ((float)(blobs[i].InvertedEstimatedCursorX - 1) / (float)grownRegions.Width) - 1;
                            a.Y = (2 * ((float)(blobs[i].InvertedEstimatedCursorY - 1) / (float)grownRegions.Height) - 1) * (-1);
                            b.X = 2 * ((float)(blobs[i].InvertedEstimatedCursorX + 1) / (float)grownRegions.Width) - 1;
                            b.Y = (2 * ((float)(blobs[i].InvertedEstimatedCursorY + 1) / (float)grownRegions.Height) - 1) * (-1);

                            bordersDetectShader.SolidFillColor = new Vector3((byte)1, (byte)0, (byte)1);
                            bordersDetectShader.CurrentTechnique.Passes[0].Apply();

                            Host.CurrentEngine.Renderer.QuadRenderer.Render(ref a, ref b, 0);

                            Host.CurrentEngine.Renderer.RendererSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);

                            var l_pos = new Vector2((float)blobs[i].AvgCenterX, (float)blobs[i].AvgCenterY);

                            LineBatch.DrawLine(Host.CurrentEngine.Renderer.RendererSpriteBatch, Microsoft.Xna.Framework.Color.White,
                                l_pos,
                                l_pos +
                                Vector2.UnitX * (float)Math.Cos(blobs[i].AverageDirection) * 10f +
                                Vector2.UnitY * (float)Math.Sin(blobs[i].AverageDirection) * -10f);

                            LineBatch.DrawLine(Host.CurrentEngine.Renderer.RendererSpriteBatch, Microsoft.Xna.Framework.Color.Green,
                                l_pos,
                                l_pos +
                                Vector2.UnitX * (float)Math.Cos(blobs[i].PrincipalDirection) * 10f +
                                Vector2.UnitY * (float)Math.Sin(blobs[i].PrincipalDirection) * -10f);

                            Host.CurrentEngine.Renderer.RendererSpriteBatch.End();
                        }
                    }

                    device.RasterizerState = oldRasterizer;
                }
                Host.CurrentEngine.RenderTargetManager.Pop();

#warning Image saving disabled

#if(IGNORE)
                PrintDebugImage("blobs-4", grownRegions);

                using (var stream = new FileStream(ResolveDebugOutputFileName("stats-final") + ".txt", FileMode.Create))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine(String.Format("Blobs count : {0}", blobCount));
                        writer.WriteLine("---------------");
                        writer.WriteLine();

                        for (int i = 0; i < maxMainBlobsCount; i++)
                        {
                            writer.WriteLine(String.Format("Blob {0}", i));
                            var blob = mainBlobs[i];

                            if (blob == null)
                                writer.WriteLine("NULL");
                            else
                            {
                                writer.WriteLine("Depth : {0}", blob.AverageDepth);
                                writer.WriteLine("Average Direction : {0}", blob.AverageDirection);
                                writer.WriteLine("Principal Direction : {0}", blob.PrincipalDirection);
                                writer.WriteLine("Center X : {0}", blob.AvgCenterX);
                                writer.WriteLine("Center Y : {0}", blob.AvgCenterY);
                                writer.WriteLine("Cursor X : {0}", blob.EstimatedCursorX);
                                writer.WriteLine("Cursor Y : {0}", blob.EstimatedCursorX);
                                writer.WriteLine("Inverted Cursor X : {0}", blob.InvertedEstimatedCursorX);
                                writer.WriteLine("Inverted Cursor Y : {0}", blob.InvertedEstimatedCursorY);
                            }


                            writer.WriteLine();
                            writer.WriteLine();
                            writer.WriteLine();
                        }
                    }
                }
#endif
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
