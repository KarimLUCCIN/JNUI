using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.ImageProcessing;
using KinectBrowser.Interaction;
using KinectBrowser.Interaction.Gestures;

namespace KinectBrowser.Input.Kinect
{
    public class KinectGesturesTracker
    {
        public Dictionary<BlobsTracker.TrackedBlob, BlobGestureFollower> currentBlobs = new Dictionary<BlobsTracker.TrackedBlob, BlobGestureFollower>();

        private List<RecognizedGesture> recognizedGestures = new List<RecognizedGesture>();

        private TimeSpan gestureDetectionLatency = TimeSpan.FromMilliseconds(500);

        public TimeSpan GestureDetectionLatency
        {
            get { return gestureDetectionLatency; }
            set { gestureDetectionLatency = value; }
        }
        
        public KinectGesturesTracker()
        {

        }

        public void RecordSingleRecognizedGesture(RecognizedGestureEventHandler actionDelegate, params SimpleGesture[] gestures)
        {
            if (actionDelegate != null)
            {
                var gestureReco = new RecognizedGesture(gestures);
                gestureReco.Activated += delegate(object sender, RecognizedGestureEventArgs e)
                {
                    actionDelegate(sender, e);
                };
                RecordRecognizedGesture(gestureReco);
            }
        }

        public void RecordRecognizedGesture(RecognizedGesture gestureReco)
        {
            if (gestureReco == null)
                throw new ArgumentNullException("gestureReco");

            if (!recognizedGestures.Contains(gestureReco))
                recognizedGestures.Add(gestureReco);
        }

        public void Update(IEnumerable<BlobsTracker.TrackedBlob> updatedBlobs)
        {
            /* Rajouter les nouveaux blobs */
            foreach(var item in updatedBlobs)
            {
                if(item != null && !currentBlobs.ContainsKey(item))
                    currentBlobs[item] = new BlobGestureFollower(item, 10, 10, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(200));
            }

            /* Retirer ceux qui sont morts, mettre à jour les autres */
            var dedBlobs = new List<BlobsTracker.TrackedBlob>();
            foreach (var tracked in currentBlobs)
            {
                tracked.Value.Update();

                if (tracked.Value.State == Interaction.CursorState.Default)
                {
                    /* Il est mort, jim ! */
                    dedBlobs.Add(tracked.Key);
                }
                else
                {
                    /* On essaye les détections */
                    TryDetect(tracked.Value);
                }
            }

            /* Ménage */
            foreach (var ded in dedBlobs)
                currentBlobs.Remove(ded);
        }

        private void TryDetect(BlobGestureFollower follower)
        {
            if (follower == null)
                throw new ArgumentNullException("follower");

            var scoresForEachMoves = 
                from desiredGesture in recognizedGestures 
                select new { gesture = desiredGesture, score = ComputeGestureScore(DateTime.Now, follower, desiredGesture) };

            var bestMatch =
                (
                from match in scoresForEachMoves
                where match.score > 0
                orderby match.score descending
                select match.gesture
                ).FirstOrDefault();

            if (bestMatch != null)
            {
                follower.Sequence.Reset();

                bestMatch.RaiseActivated(follower.Blob);
            }
        }

        private double ComputeGestureScore(DateTime now, BlobGestureFollower follower, RecognizedGesture desiredGesture)
        {
            var sequence = follower.Sequence;

            if ((now - sequence.LastModificationTime) >= gestureDetectionLatency)
            {
                var machine = desiredGesture.Machine;
                machine.Reset();

                for (int i = 0; i < sequence.Count && !machine.Valid; i++)
                {
                    var gesture = sequence[sequence.Count - i - 1];
                    if (gesture != null)
                    {
                        machine.Update(gesture.Gesture);
                    }
                }

                return machine.Valid ? machine.Score : 0;
            }
            else
                return 0;
        }
    }
}
