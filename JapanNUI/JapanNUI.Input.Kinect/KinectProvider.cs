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

        public KinectProvider(IInputListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            Listener = listener;
            Enabled = true;

            nui = new Runtime();

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


            nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

            leftHandProvider = new KinectPositionProvider(this);
            rightHandProvider = new KinectPositionProvider(this);

            providers = new KinectPositionProvider[] { leftHandProvider, rightHandProvider };
        }

        public void Shutdown()
        {
            nui.Uninitialize();
        }

        void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            //PlanarImage Image = e.ImageFrame.Image;
            //byte[] convertedDepthFrame = convertDepthFrame(Image.Bits);

            //depth.Source = BitmapSource.Create(
            //    Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, convertedDepthFrame, Image.Width * 4);

            //++totalFrames;

            //DateTime cur = DateTime.Now;
            //if (cur.Subtract(lastTime) > TimeSpan.FromSeconds(1))
            //{
            //    int frameDiff = totalFrames - lastFrames;
            //    lastFrames = totalFrames;
            //    lastTime = cur;
            //    frameRate.Text = frameDiff.ToString() + " fps";
            //}
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

        //Polyline getBodySegment(Microsoft.Research.Kinect.Nui.JointsCollection joints, Brush brush, params JointID[] ids)
        //{
        //    PointCollection points = new PointCollection(ids.Length);
        //    for (int i = 0; i < ids.Length; ++i)
        //    {
        //        points.Add(getDisplayPosition(joints[ids[i]], skeleton.Width, skeleton.Height));
        //    }

        //    Polyline polyline = new Polyline();
        //    polyline.Points = points;
        //    polyline.Stroke = brush;
        //    polyline.StrokeThickness = 5;
        //    return polyline;
        //}

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame skeletonFrame = e.SkeletonFrame;

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

                        leftHandProvider.Update(new Vector3(pt, 0));

                        b = true;
                    }

                    var rightHand = SelectHand(firstSq, JointID.HandRight);

                    if (rightHand != null && rightHand.HasValue)
                    {
                        var pt = getDisplayPosition(rightHand.Value);

                        rightHandProvider.Update(new Vector3(pt, 0));

                        b = true;
                    }

                    if (b)
                        break;
                }
            }

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
