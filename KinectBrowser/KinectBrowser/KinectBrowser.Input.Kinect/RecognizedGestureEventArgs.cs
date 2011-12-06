using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectBrowser.Input.Kinect
{
    public class RecognizedGestureEventArgs : EventArgs
    {
        public ImageProcessing.BlobsTracker.TrackedBlob Origin { get; set; }
    }
}
