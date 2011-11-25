using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.ImageProcessing.SectionsBuilders;

namespace KinectBrowser.ImageProcessing
{
    public class BlobsTracker
    {
        public enum Status
        {
            Lost,

            Tracking,

            Waiting
        }

        public class TrackedBlob
        {
            public Status Status { get; internal set; }

            public ManagedBlob Current { get; internal set; }

            public int Age { get; set; }

            internal int waitingCycles = 0;

            internal bool attached = false;

            public bool InvertedCursor { get; internal set; }
        }

        internal class TrackedBlobIntermediate
        {
            public ManagedBlob blob;

            public double score;
        }

        public List<TrackedBlob> TrackedBlobs { get; private set; }

        public BlobsTracker()
        {
            TrackedBlobs = new List<TrackedBlob>();
        }

        private List<TrackedBlobIntermediate> intermediatesData = new List<TrackedBlobIntermediate>();

        private int maxWaitingCycles = 50;

        public int MaxWaitingCycles
        {
            get { return maxWaitingCycles; }
            set { maxWaitingCycles = value; }
        }

        private double maxBlobsDistance = 50;

        public double MaxBlobsDistance
        {
            get { return maxBlobsDistance; }
            set { maxBlobsDistance = value; }
        }
                
        public void Update(IEnumerable<ManagedBlob> blobs)
        {
            /* Préparation */
            for (int i = 0; i < TrackedBlobs.Count;i++ )
            {
                var tracked = TrackedBlobs[i];

                if (tracked.Status == Status.Lost)
                {
                    TrackedBlobs.RemoveAt(i);
                    i--;
                }
                else
                {
                    tracked.attached = false;
                    tracked.Age++;
                }
            }

            /* Créer les valeurs intermédiaires */
            intermediatesData.Clear();

            foreach (var blob in blobs)
            {
                intermediatesData.Add(new TrackedBlobIntermediate() { blob = blob });
            }

            /* Calculer les scores pour les blobs actuellement traqués */
            foreach (var i_blob in intermediatesData)
            {
                i_blob.score = double.MaxValue;

                foreach (var actual in TrackedBlobs)
                {
                    i_blob.score = Math.Min(i_blob.score, Distance(i_blob.blob.AvgCenterX, i_blob.blob.AvgCenterY, actual.Current.AvgCenterX, actual.Current.AvgCenterY));
                }

                if (i_blob.score > maxBlobsDistance)
                    i_blob.score = double.MaxValue;
            }

            /* Sort et assignation des blobs les plus proches en priorité */
            SortIntermediateData();

            foreach (var i_blob in intermediatesData)
            {
                if(i_blob.score == double.MaxValue)
                    break;

                TrackedBlob closest = null;
                double score = double.MaxValue;
                bool closestIsInverted = false;

                foreach (var actual in TrackedBlobs)
                {
                    if (!actual.attached)
                    {
                        bool canConsiderInverted = actual.Current.Width > actual.Current.Height;

                        if (!canConsiderInverted && actual.InvertedCursor)
                            actual.InvertedCursor = false;

                        var directDistance = Distance(i_blob.blob.EstimatedCursorX, i_blob.blob.EstimatedCursorY, actual.Current.EstimatedCursorX, actual.Current.EstimatedCursorY);
                        var invertedDistance = Distance(i_blob.blob.InvertedEstimatedCursorX, i_blob.blob.InvertedEstimatedCursorY, actual.Current.EstimatedCursorX, actual.Current.EstimatedCursorY);

                        if (actual.InvertedCursor)
                        {
                            var tmp = directDistance;
                            directDistance = invertedDistance;
                            invertedDistance = tmp;
                        }

                        double d;
                        bool invert;

                        if (directDistance <= invertedDistance || !canConsiderInverted)
                        {
                            d = directDistance;
                            invert = false;
                        }
                        else
                        {
                            d = invertedDistance;
                            invert = true;
                        }

                        if (d < score)
                        {
                            closest = actual;
                            score = d;
                            closestIsInverted = invert;
                        }
                    }
                }

                if (score < maxBlobsDistance)
                {
                    closest.Current = i_blob.blob.Clone();
                    closest.Status = Status.Tracking;
                    closest.attached = true;
                    closest.waitingCycles = 0;
                    closest.InvertedCursor = closestIsInverted;
                }
            }

            /* Attribute Lost value */
            foreach (var blob in TrackedBlobs)
            {
                if (!blob.attached)
                {
                    if (blob.waitingCycles > maxWaitingCycles)
                        blob.Status = Status.Lost;
                    else
                    {
                        blob.Status = Status.Waiting;
                        blob.waitingCycles++;
                    }
                }
            }

            /* On rajoute les nouveaux blobs */
            foreach (var i_blob in intermediatesData)
            {
                if (i_blob.score >= maxBlobsDistance)
                {
                    TrackedBlobs.Add(new TrackedBlob() { Current = i_blob.blob, attached = true, Status = Status.Tracking });
                }
            }
        }

        private void SortIntermediateData()
        {
            intermediatesData.Sort((a, b) => a.score.CompareTo(b.score));
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            var a = x2 - x1;
            var b = y2 - y1;

            return Math.Sqrt(a * a + b * b);
        }
    }
}
