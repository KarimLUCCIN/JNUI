using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.ImageProcessing;
using KinectBrowser.Interaction.Gestures;

namespace KinectBrowser.Input.Kinect
{
    public class BlobGestureFollower
    {
        public BlobsTracker.TrackedBlob Blob { get; internal set; }

        public GesturePoint AssociatedPoint { get; private set; }

        public GestureSequence Sequence { get; private set; }

        public GestureManager Manager { get; private set; }

        public Interaction.CursorState State
        {
            get { return AssociatedPoint.State; }
        }

        public BlobGestureFollower(BlobsTracker.TrackedBlob blob, int historySize, int pixelMoveThreshold, TimeSpan maxGestureDuration, TimeSpan simpleGestureLatency)
        {
            if (blob == null)
                throw new ArgumentNullException("blob");

            Blob = blob;

            Sequence = new GestureSequence(maxGestureDuration);
            Manager = new GestureManager(simpleGestureLatency);

            AssociatedPoint = new GesturePoint();
            AssociatedPoint.HistorySize = historySize;
            AssociatedPoint.PixelMoveTreshold = pixelMoveThreshold;
        }

        public void Update()
        {
            if(Blob.Status == BlobsTracker.Status.Tracking)
                AssociatedPoint.UpdatePosition(new Microsoft.Xna.Framework.Vector3( Blob.Cursor, 0), ParseState(Blob.Status));

            Manager.Update(AssociatedPoint);

            if(!Manager.Idle)
                Sequence.Enqueue(new SimpleGestureKey() { Gesture = Manager.CurrentGesture, Time = DateTime.Now });
        }

        private Interaction.CursorState ParseState(BlobsTracker.Status status)
        {
            switch (status)
            {
                default:
                case BlobsTracker.Status.Lost:
                    return Interaction.CursorState.Default;
                case BlobsTracker.Status.Tracking:
                    return Interaction.CursorState.Tracked;
                case BlobsTracker.Status.Waiting:
                    return Interaction.CursorState.StandBy;
            }
        }
    }
}
