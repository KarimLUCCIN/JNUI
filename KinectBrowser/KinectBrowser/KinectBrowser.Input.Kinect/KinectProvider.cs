//#define RETRHOW_RUNTIME_EXCEPTION
#define DISABLE_DEPTH_VIEW

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction;
using Microsoft.Research.Kinect.Nui;
using KinectBrowser.Interaction.Maths;
using KinectBrowser.ImageProcessing;
using System.Runtime.InteropServices;
using KinectBrowser.D3D;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Input.Kinect
{
    public class KinectProvider : IInputProvider
    {
        private Runtime nui;
        public IInputClient Client { get; private set; }

        private IPositionProvider[] providers;

        private KinectPositionProvider leftHandProvider;
        private KinectPositionProvider rightHandProvider;

        public bool Available { get; private set; }

        private double closestPointUpdateLatency = 0.5;

        public double ClosestPointUpdateLatency
        {
            get { return closestPointUpdateLatency; }
            set { closestPointUpdateLatency = value; }
        }

        public ImageProcessingEngine ImageProcessingEngine { get; private set; }
        public KinectBlobsMatcher KinectBlobsMatcher { get; private set; }

        public static bool HasKinects
        {
            get
            {
                try
                {
                    return Runtime.Kinects.Count > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public KinectProvider(SoraEngineHost host, IInputClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            Client = client;
            Enabled = true;
            Available = true;

            try
            {
                nui = Runtime.Kinects[0];

#if(RETRHOW_RUNTIME_EXCEPTION)
                try
                {
                    nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                }


                try
                {
                    nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException("Failed to open stream. Please make sure to specify a supported image type and resolution.");
                }
#else
                nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex);// | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);

                nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
#endif


                nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);

                leftHandProvider = new KinectPositionProvider("left", this);
                rightHandProvider = new KinectPositionProvider("right", this);

                providers = new KinectPositionProvider[] { rightHandProvider, leftHandProvider };
            }
            catch
            {
                Available = false;
            }

            if (Available)
            {
                try
                {
                    ImageProcessingEngine = new ImageProcessingEngine(host, 320, 240, 4);
                    KinectBlobsMatcher = new KinectBlobsMatcher(ImageProcessingEngine, ImageProcessingEngine.DataWidth, ImageProcessingEngine.DataHeight);
                }
                catch
                {
                    KinectBlobsMatcher = null;
                    ImageProcessingEngine = null;
                    Available = false;
                }
            }
        }

        public void Shutdown()
        {
            if (Available)
            {
                nui.Uninitialize();

                ImageProcessingEngine.Dispose();
            }
        }

        // We want to control how depth data gets converted into false-color data
        // for more intuitive visualization, so we keep 32-bit color frame buffer versions of
        // these, to be updated whenever we receive and process a 16-bit frame.
        const int RED_IDX = 2;
        const int GREEN_IDX = 1;
        const int BLUE_IDX = 0;

        byte[] depthFrame32 = new byte[320 * 240 * 4];
        byte[] depthFilteredFrame32 = new byte[320 * 240 * 4];

        // Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame
        // that displays different players in different colors
        unsafe byte[] convertDepthFrame(byte[] depthFrame16)
        {
            int minDepth = int.MaxValue;
            int maxDepth = int.MinValue;
            int minDepthIndex = 0;

            //byte[] fData = new byte[4];

            var d16Length = depthFrame16.Length;
            var d32Length = depthFrame32.Length;

            fixed (byte* pdepthFilteredFrame32 = depthFilteredFrame32)
            {
                fixed (byte* pdepthFrame16 = depthFrame16)
                {
                    for (int i16 = 0, i32 = 0; i16 < d16Length && i32 < d32Length; i16 += 2, i32 += 4)
                    {
                        //int player = pdepthFrame16[i16] & 0x07;
                        int realDepth = (pdepthFrame16[i16 + 1] << 5) | (pdepthFrame16[i16] >> 3);

                        float fDepth = realDepth;

                        //VectorUtils.BytesFromFloat(realDepth, fData);

                        //depthFilteredFrame32[i32 + 0] = fData[0];// (byte)(realDepth & 0x000000FF);
                        //depthFilteredFrame32[i32 + 1] = fData[1];// (byte)((realDepth & 0x0000FF00) >> 8);
                        //depthFilteredFrame32[i32 + 2] = fData[2];// (byte)((realDepth & 0x0000FF00) >> 8);
                        //depthFilteredFrame32[i32 + 3] = fData[3];// (byte)((realDepth & 0x0000FF00) >> 8);

                        //depthFilteredFrame32[i32 + 0] = *((byte*)(&fDepth) + 0);
                        //depthFilteredFrame32[i32 + 1] = *((byte*)(&fDepth) + 1);
                        //depthFilteredFrame32[i32 + 2] = *((byte*)(&fDepth) + 2);
                        //depthFilteredFrame32[i32 + 3] = *((byte*)(&fDepth) + 3);

                        *((float*)&pdepthFilteredFrame32[i32 + 0]) = fDepth;

                        if (minDepth > realDepth && realDepth > 0)
                        {
                            minDepthIndex = i32;
                            minDepth = realDepth;
                        }

                        maxDepth = maxDepth > realDepth ? maxDepth : realDepth;
                    }
                }
            }

            if (KinectBlobsMatcher != null)
                KinectBlobsMatcher.Process(depthFilteredFrame32, minDepth, maxDepth);

            return depthFrame32;
        }

        bool processing = false;
        object sync = new object();

        DateTime lastFrameTime = DateTime.MinValue;

        void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            lock (sync)
            {
                var msSinceLast = (DateTime.Now - lastFrameTime).TotalMilliseconds;

                if (processing || msSinceLast < 1 / 30.0)
                    return;

                lastFrameTime = DateTime.Now;

                processing = true;
            }

            if (KinectBlobsMatcher != null)
            {
                PlanarImage Image = e.ImageFrame.Image;
                byte[] convertedDepthFrame = convertDepthFrame(Image.Bits);

                leftHandProvider.Update(KinectBlobsMatcher.LeftHandBlob.MBlob, new Vector3(KinectBlobsMatcher.LeftHandBlob.CursorPosition.X, KinectBlobsMatcher.LeftHandBlob.CursorPosition.Y, (float)(KinectBlobsMatcher.LeftHandBlob.AverageDepth)), ParseKinectCursorState(KinectBlobsMatcher.LeftHandBlob));
                rightHandProvider.Update(KinectBlobsMatcher.RightHandBlob.MBlob, new Vector3(KinectBlobsMatcher.RightHandBlob.CursorPosition.X, KinectBlobsMatcher.RightHandBlob.CursorPosition.Y, (float)(KinectBlobsMatcher.RightHandBlob.AverageDepth)), ParseKinectCursorState(KinectBlobsMatcher.RightHandBlob));
            }

            lock (sync)
            {
                processing = false;
            }
        }

        private CursorState ParseKinectCursorState(KinectBlobsMatcher.BlobParametersRecord blobRecord)
        {
            if (blobRecord == null || blobRecord.MBlob == null)
                return CursorState.Default;
            else
            {
                var mblob = blobRecord.MBlob;
                switch (mblob.Status)
                {
                    default:
                    case BlobsTracker.Status.Lost:
                        return CursorState.Default;
                    case BlobsTracker.Status.Tracking:
                        return CursorState.Tracked;
                    case BlobsTracker.Status.Waiting:
                        return CursorState.StandBy;
                }
            }
        }

        #region IInputProvider Members

        public bool Enabled { get; set; }

        public IPositionProvider[] Positions
        {
            get { return providers; }
        }

        public int Priority
        {
            get { return 0; }
        }
                
        public void Update()
        {

        }

        public TimeSpan ProcessingTime
        {
            get
            {
                if (ImageProcessingEngine != null)
                    return ImageProcessingEngine.ProcessingTime;
                else
                    return TimeSpan.Zero;
            }
        }

        IPositionProvider mainPosition = null;

        BlobsTracker.TrackedBlob clickBlob = null;

        public BlobsTracker.TrackedBlob ClickBlob
        {
            get { return clickBlob; }
        }

        BlobsTracker.TrackedBlob mainBlob;

        public BlobsTracker.TrackedBlob MainBlob
        {
            get { return mainBlob; }
        }

        public void ForceCursorAquire(BlobsTracker.TrackedBlob blob)
        {
            KinectBlobsMatcher.LeftHandBlob.MBlob = null;
            KinectBlobsMatcher.RightHandBlob.MBlob = blob;
        }

        public void ForceCursorRelease()
        {
            KinectBlobsMatcher.LeftHandBlob.MBlob = null;
            KinectBlobsMatcher.RightHandBlob.MBlob = null;
        }

        /// <summary>
        /// Tout blob répondant aux conditions du click mais
        /// étant plus agé que cette date sera ignoré.
        /// 
        /// Il sert afin d'éviter que les objets fixes ne soient considérés
        /// comme un click ...
        /// </summary>
        DateTime minimumClickBlobCreationDate = DateTime.Now;
        
        public IPositionProvider MainPosition
        {
            get
            {
                if (mainPosition == null)
                {
                    SelectMainPosition();
                }
                else
                {
                    if (mainPosition.CurrentPoint.State == CursorState.Default)
                    {
                        SelectMainPosition();
                    }
                }

                if (mainPosition.CurrentPoint.State != CursorState.Tracked)
                {
                    mainPosition.LeftButtonClicked = false;
                    clickBlob = null;
                    mainBlob = null;
                }
                else
                {
                    mainBlob = (mainPosition == leftHandProvider) ? KinectBlobsMatcher.LeftHandBlob.MBlob : KinectBlobsMatcher.RightHandBlob.MBlob; ;

                    if (clickBlob != null)
                    {
                        if (clickBlob.Status != BlobsTracker.Status.Tracking)
                        {
                            mainPosition.LeftButtonClicked = false;
                            clickBlob = null;
                        }
                        else
                        {
                            if ((mainPosition == leftHandProvider && clickBlob == KinectBlobsMatcher.LeftHandBlob.MBlob) ||
                                (mainPosition == rightHandProvider && clickBlob == KinectBlobsMatcher.RightHandBlob.MBlob))
                            {
                                mainPosition.LeftButtonClicked = false;
                                clickBlob = null;
                            }
                            else
                                mainPosition.LeftButtonClicked = true;
                        }
                    }
                    else
                    {
                        var forbiddenBlob = (mainPosition == leftHandProvider) ? KinectBlobsMatcher.LeftHandBlob.MBlob : KinectBlobsMatcher.RightHandBlob.MBlob;

                        var candidates = (from blob in KinectBlobsMatcher.AdditionnalBlobs
                                         where blob != forbiddenBlob
                                         select blob).ToList();

                        /* on ajoute aussi le blob non utilisé pour l'autre main */
                        if (mainPosition == leftHandProvider && KinectBlobsMatcher.RightHandBlob.MBlob != null)
                            candidates.Add(KinectBlobsMatcher.RightHandBlob.MBlob);
                        else if (mainPosition == rightHandProvider && KinectBlobsMatcher.LeftHandBlob.MBlob != null)
                            candidates.Add(KinectBlobsMatcher.LeftHandBlob.MBlob);

                        candidates.Sort((a, b) => a.Age.CompareTo(b.Age));

                        /* on prend le plus jeune, mais on attend le prochain tour pour cliquer */
                        clickBlob = (from candidate in candidates where candidate.CreationTime > minimumClickBlobCreationDate select candidate).FirstOrDefault();

                        /* on met à jour l'age minimal du blob de click */
                        if (clickBlob != null)
                            minimumClickBlobCreationDate = clickBlob.CreationTime;
                    }
                }

                return mainPosition;
            }
        }

        /// <summary>
        /// Si le blob principal est croisé avec un autre, inverse les deux
        /// </summary>
        public void SwitchMainBlobWitchCrossedOne()
        {
            if (mainBlob != null && mainBlob.Crossed)
            {
                var oldMainBlob = mainBlob;
                var target = KinectBlobsMatcher.BlobsTracker.FindCrossedTarget(mainBlob);

                if (target != null)
                {
                    if (mainBlob == KinectBlobsMatcher.LeftHandBlob.MBlob)
                    {
                        KinectBlobsMatcher.LeftHandBlob.MBlob = target;

                        if (KinectBlobsMatcher.RightHandBlob.MBlob == target)
                            KinectBlobsMatcher.RightHandBlob.MBlob = oldMainBlob;
                    }
                    else
                    {
                        KinectBlobsMatcher.RightHandBlob.MBlob = target;

                        if (KinectBlobsMatcher.LeftHandBlob.MBlob == target)
                            KinectBlobsMatcher.LeftHandBlob.MBlob = oldMainBlob;
                    }
                }
            }
        }

        private IPositionProvider SelectMainPosition()
        {
            if (rightHandProvider.CurrentPoint.State == CursorState.Tracked)
                return mainPosition = rightHandProvider;
            else if (leftHandProvider.CurrentPoint.State == CursorState.Tracked)
                return mainPosition = leftHandProvider;
            else
                return mainPosition = rightHandProvider;
        }

        #endregion
    }
}
