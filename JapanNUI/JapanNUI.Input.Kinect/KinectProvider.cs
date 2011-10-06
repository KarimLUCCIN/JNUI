//#define RETRHOW_RUNTIME_EXCEPTION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;
using Microsoft.Research.Kinect.Nui;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Input.Kinect
{
    public class KinectProvider : IInputProvider
    {
        private Runtime nui;
        public IInputListener Listener { get; private set; }

        private IPositionProvider[] providers;

        private KinectPositionProvider leftHandProvider;
        private KinectPositionProvider rightHandProvider;

        public bool Available { get; private set; }

        public KinectProvider(IInputListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            Listener = listener;
            Enabled = true;
            Available = true;

            try
            {
                nui = new Runtime();

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
                nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);

                nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
#endif


                nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
                nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

                leftHandProvider = new KinectPositionProvider("left", this);
                rightHandProvider = new KinectPositionProvider("right", this);

                providers = new KinectPositionProvider[] { leftHandProvider, rightHandProvider };
            }
            catch
            {
                Available = false;
            }
        }

        public void Shutdown()
        {
            if (Available)
                nui.Uninitialize();
        }

        // We want to control how depth data gets converted into false-color data
        // for more intuitive visualization, so we keep 32-bit color frame buffer versions of
        // these, to be updated whenever we receive and process a 16-bit frame.
        const int RED_IDX = 2;
        const int GREEN_IDX = 1;
        const int BLUE_IDX = 0;

        byte[] depthFrame32 = new byte[320 * 240 * 4];

        // Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame
        // that displays different players in different colors
        byte[] convertDepthFrame(byte[] depthFrame16)
        {
            for (int i16 = 0, i32 = 0; i16 < depthFrame16.Length && i32 < depthFrame32.Length; i16 += 2, i32 += 4)
            {
                int player = depthFrame16[i16] & 0x07;
                int realDepth = (depthFrame16[i16 + 1] << 5) | (depthFrame16[i16] >> 3);
                // transform 13-bit depth information into an 8-bit intensity appropriate
                // for display (we disregard information in most significant bit)
                byte intensity = (byte)(255 - (255 * realDepth / 0x0fff));

                depthFrame32[i32 + RED_IDX] = 0;
                depthFrame32[i32 + GREEN_IDX] = 0;
                depthFrame32[i32 + BLUE_IDX] = 0;

                // choose different display colors based on player
                switch (player)
                {
                    case 0:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity / 2);
                        break;
                    case 1:
                        depthFrame32[i32 + RED_IDX] = intensity;
                        break;
                    case 2:
                        depthFrame32[i32 + GREEN_IDX] = intensity;
                        break;
                    case 3:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 4);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 4:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity / 4);
                        break;
                    case 5:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 4);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 6:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 7:
                        depthFrame32[i32 + RED_IDX] = (byte)(255 - intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(255 - intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(255 - intensity);
                        break;
                }
            }
            return depthFrame32;
        }

        void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            PlanarImage Image = e.ImageFrame.Image;
            byte[] convertedDepthFrame = convertDepthFrame(Image.Bits);

            Listener.DebugDisplayBgr32DepthImage(Image.Width, Image.Height, convertedDepthFrame, Image.Width * 4);
        }

        private Vector2 getDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            nui.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
            depthX = depthX * 320; //convert to 320, 240 space
            depthY = depthY * 240; //convert to 320, 240 space
            int colorX, colorY;
            ImageViewArea iv = new ImageViewArea();
            // only ImageResolution.Resolution640x480 is supported at this point
            nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);

            // map back to skeleton.Width & skeleton.Height
            return new Vector2((colorX / 640.0), (colorY / 480.0));
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame skeletonFrame = e.SkeletonFrame;

            bool hasUpdate = false;

            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    bool b = false;

                    var firstSq = data;

                    var leftHand = SelectHand(firstSq, JointID.HandLeft);

                    if (leftHand != null && leftHand.HasValue)
                    {
                        var pt = getDisplayPosition(leftHand.Value);

                        hasUpdate = hasUpdate || leftHandProvider.Update(new Vector3(pt, 0));

                        b = true;
                    }

                    var rightHand = SelectHand(firstSq, JointID.HandRight);

                    if (rightHand != null && rightHand.HasValue)
                    {
                        var pt = getDisplayPosition(rightHand.Value);

                        hasUpdate = hasUpdate || rightHandProvider.Update(new Vector3(pt, 0));

                        b = true;
                    }

                    if (b)
                        break;
                }
            }

            if (hasUpdate)
                Listener.Update(this);
        }

        private Joint? SelectHand(SkeletonData firstSq, JointID id)
        {
            return (from Joint j in firstSq.Joints
                    where (j.ID == id)
                    select new Nullable<Joint>(j)).FirstOrDefault();
        }

        #region IInputProvider Members

        public bool Enabled { get; private set; }

        public IPositionProvider[] Positions
        {
            get { return providers; }
        }

        #endregion
    }
}
