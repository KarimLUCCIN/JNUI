using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Gestures;
using KinectBrowser.Interaction.Recognition;
using KinectBrowser.ImageProcessing;

namespace KinectBrowser.Input.Kinect
{
    public class RecognizedGesture
    {
        public RecognizedGesture(params SimpleGesture[] gestures)
        {
            if (gestures == null || gestures.Length < 1)
                throw new ArgumentNullException("gestures");

            Gesture = gestures;

            Machine = new RecognitionSequenceMachine(gestures);
        }

        public SimpleGesture[] Gesture { get; internal set; }

        internal RecognitionSequenceMachine Machine { get; set; }

        internal double LastScore { get; set; }

        public event RecognizedGestureEventHandler Activated;

        internal void RaiseActivated(BlobsTracker.TrackedBlob origin)
        {
            if (Activated != null)
                Activated(this, new RecognizedGestureEventArgs() { Origin = origin });
        }
    }
}
