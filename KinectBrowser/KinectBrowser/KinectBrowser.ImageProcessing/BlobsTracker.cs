﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.ImageProcessing.SectionsBuilders;
using Microsoft.Xna.Framework;

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
            public TrackedBlob()
            {
                IsHighPriority = false;
                CreationTime = DateTime.Now;
            }

            Status status = Status.Waiting;

            public Status Status
            {
                get
                {
                    if (status == BlobsTracker.Status.Tracking)
                    {
                        if (Current == null ||
                            double.IsNaN(Current.EstimatedCursorX) ||
                            double.IsNaN(Current.EstimatedCursorY) ||
                            double.IsNaN(Current.InvertedEstimatedCursorX) ||
                            double.IsNaN(Current.InvertedEstimatedCursorY))
                        {
                            status = BlobsTracker.Status.Lost;
                        }
                    }

                    return status;
                }
                internal set { status = value; }
            }

            public ManagedBlob Current { get; internal set; }

            public int Age { get; set; }

            public DateTime CreationTime;

            internal int waitingCycles = 0;

            public int WaitingCycles
            {
                get { return waitingCycles; }
            }

            internal bool attached = false;

            public bool InvertedCursor { get; internal set; }

            public Vector2 Center
            {
                get { return Current == null ? Vector2.Zero : new Vector2((float)Current.AvgCenterX, (float)Current.AvgCenterY); }
            }

            public Vector2 Cursor
            {
                get
                {
                    return Current == null ? Vector2.Zero :
                        (InvertedCursor
                            ? new Vector2((float)Current.InvertedEstimatedCursorX, (float)Current.InvertedEstimatedCursorY)
                            : new Vector2((float)Current.EstimatedCursorX, (float)Current.EstimatedCursorY));
                }
            }

            public Vector2 LinearCursor
            {
                get
                {
                    return Current == null ? Vector2.Zero :
                        (InvertedCursor
                            ? new Vector2((float)Current.LinearInvertedEstimatedCursorX, (float)Current.LinearInvertedEstimatedCursorY)
                            : new Vector2((float)Current.LinearEstimatedCursorX, (float)Current.LinearEstimatedCursorY));
                }
            }

            public bool Crossed
            {
                get { return Current == null ? false : Current.Crossed; }
            }

            public bool IsHighPriority { get; set; }
        }

        internal class TrackedBlobIntermediate
        {
            public ManagedBlob blob;

            public double score;

            public bool used = false;
        }

        internal class TrackedBlobEvaluator
        {
            public TrackedBlob blob;

            public TrackedBlobIntermediate closest;

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

        List<TrackedBlobEvaluator> waitingBlobs = new List<TrackedBlobEvaluator>();
        List<TrackedBlobIntermediate> newBlobs = new List<TrackedBlobIntermediate>();

        /// <summary>
        /// Si ce blob est croisé avec un autre, renvoi l'autre blob correspondant. Sinon, null.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        public TrackedBlob FindCrossedTarget(TrackedBlob blob)
        {
            if (blob.Crossed && blob.Current != null)
            {
                return (from l_blob in TrackedBlobs where blob.Current.Equals(l_blob.Current) select l_blob).FirstOrDefault();
            }
            else
                return null;
        }

        float highPriorityMultiplier = 0.6f;

        public void Update(IEnumerable<ManagedBlob> blobs)
        {
            /* Préparation */
            for (int i = 0; i < TrackedBlobs.Count; i++)
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
                intermediatesData.Add(new TrackedBlobIntermediate() { blob = blob, used = false });
            }

            /* Calculer les scores pour les blobs actuellement traqués */
            foreach (var i_blob in intermediatesData)
            {
                i_blob.score = double.MaxValue;

                foreach (var actual in TrackedBlobs)
                {
                    i_blob.score = Math.Min(i_blob.score, Distance(i_blob.blob.AvgCenterX, i_blob.blob.AvgCenterY, actual.Current.AvgCenterX, actual.Current.AvgCenterY));

                    /* On augmente le score des blobs prioritaires (en le diminant ...) */
                    if (actual.IsHighPriority)
                        i_blob.score *= highPriorityMultiplier; 
                }

                if (i_blob.score > maxBlobsDistance)
                    i_blob.score = double.MaxValue;
            }

            /* Sort et assignation des blobs les plus proches en priorité */
            SortIntermediateData();

            foreach (var i_blob in intermediatesData)
            {
                if (i_blob.score == double.MaxValue)
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

                        if (actual.IsHighPriority)
                            d *= highPriorityMultiplier;

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

                    i_blob.used = true;
                }
            }

            waitingBlobs.Clear();

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

                        waitingBlobs.Add(new TrackedBlobEvaluator() { blob = blob });
                    }
                }
            }

            /* On tente d'attribuer les blobs en état d'attende aux blobs les plus proches */
            if (waitingBlobs.Count > 0)
            {
                newBlobs.Clear();

                foreach (var i_blob in intermediatesData)
                {
                    if (!i_blob.used)
                    {
                        newBlobs.Add(i_blob);
                    }
                }

                if (newBlobs.Count > 0)
                {
                    while (waitingBlobs.Count > 0 && newBlobs.Count > 0)
                    {
                        ComputeAssociationScores();

                        var first = waitingBlobs[0];

                        if (first.score < 40)
                        {
                            /* pour ne pas le rajouter comme nouveau blob */
                            first.closest.score = 0;

                            first.blob.Current = first.closest.blob.Clone();
                            first.blob.Status = Status.Tracking;
                            first.blob.attached = true;
                            first.blob.waitingCycles = 0;
                            //first.blob.InvertedCursor = closestIsInverted;

                            waitingBlobs.RemoveAt(0);
                            newBlobs.Remove(first.closest);
                        }
                        else
                        {
                            /* too far, no matches */
                            break;
                        }
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

        private void ComputeAssociationScores()
        {
            foreach (var waitingBlob in waitingBlobs)
            {
                var minimumBlob = newBlobs[0];
                var minimumBlobScore = Distance(waitingBlob.blob.Current.AvgCenterX, waitingBlob.blob.Current.AvgCenterY, minimumBlob.blob.AvgCenterX, minimumBlob.blob.AvgCenterY);

                for (int i = 1; i < newBlobs.Count; i++)
                {
                    var nBlob = newBlobs[i];
                    var nDistance = Distance(waitingBlob.blob.Current.AvgCenterX, waitingBlob.blob.Current.AvgCenterY, nBlob.blob.AvgCenterX, nBlob.blob.AvgCenterY);

                    if (nDistance < minimumBlobScore)
                    {
                        minimumBlobScore = nDistance;
                        minimumBlob = nBlob;
                    }
                }

                waitingBlob.closest = minimumBlob;
                waitingBlob.score = minimumBlobScore;
            }

            waitingBlobs.Sort((a, b) => a.score.CompareTo(b.score));
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
