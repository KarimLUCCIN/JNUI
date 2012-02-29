using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.ImageProcessing;
using KinectBrowser.ImageProcessing.SectionsBuilders;
using KinectBrowser.Interaction.Maths;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Input.Kinect
{
    /// <summary>
    /// Chargé d'associé les mains aux blobs.
    /// </summary>
    public class KinectBlobsMatcher
    {
        public class BlobParametersRecord
        {
            internal bool empty = true;
            internal bool assigned = false;

            internal BlobsTracker.TrackedBlob MBlob = new BlobsTracker.TrackedBlob();

            public BlobsTracker.TrackedBlob TrackedBlob
            {
                get { return MBlob; }
            }

            internal bool useInvertedLeftCursor = false;
            internal bool useInvertedRightCursor = false;

            /// <summary>
            /// Position du curseur associé, entre (-1,-1) et (1,1)
            /// </summary>
            public Vector2 CursorPosition { get; set; }

            double lastAverageDepth = 0;

            public double AverageDepth
            {
                get { return (MBlob == null || MBlob.Current == null) ? lastAverageDepth : (lastAverageDepth = MBlob.Current.AverageDepth); }
            }
        }

        public BlobParametersRecord LeftHandBlob { get; private set; }
        public BlobParametersRecord RightHandBlob { get; private set; }

        private Vector2 idealLeftHandPosition = new Vector2(0.1f, 0.5f);
        private Vector2 idealRightHandPosition = new Vector2(0.9f, 0.5f);

        public float DataWidth { get; private set; }
        public float DataHeight { get; private set; }

        public ImageProcessingEngine ProcessingEngine { get; private set; }

        private BlobParametersRecord[] scoringBlobs;

        private float updateLatency = 0.7f;

        public float UpdateLatency
        {
            get { return updateLatency; }
            set { updateLatency = value; }
        }

        public BlobsTracker BlobsTracker { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processingEngine"></param>
        /// <param name="dataWidth">Largeur de la fenêtre de données dans laquelle se trouve les blobs</param>
        /// <param name="dataHeight">Hauteur de la fenêtre de données dans laquelle se trouve les blobs</param>
        public KinectBlobsMatcher(ImageProcessingEngine processingEngine, int dataWidth, int dataHeight)
        {
            if (processingEngine == null)
                throw new ArgumentNullException("processingEngine");

            ProcessingEngine = processingEngine;

            if (ProcessingEngine.MaxMainBlobsCount <= 1)
                throw new ArgumentOutOfRangeException("processingEngine.MaxMainBlobsCount <= 1");

            BlobsTracker = new ImageProcessing.BlobsTracker();

            DataWidth = dataWidth;
            DataHeight = dataHeight;

            LeftHandBlob = new BlobParametersRecord();
            LeftHandBlob.CursorPosition = idealLeftHandPosition;

            RightHandBlob = new BlobParametersRecord();
            RightHandBlob.CursorPosition = idealRightHandPosition;

            scoringBlobs = new BlobParametersRecord[ProcessingEngine.MaxMainBlobsCount];

            for (int i = 0; i < scoringBlobs.Length; i++)
                scoringBlobs[i] = new BlobParametersRecord();

            LeftHandBlob.CursorPosition = idealLeftHandPosition;
            RightHandBlob.CursorPosition = idealRightHandPosition;
        }

        private List<BlobParametersRecord> leftScoringSort = new List<BlobParametersRecord>();
        private List<BlobParametersRecord> rightScoringSort = new List<BlobParametersRecord>();

        List<ManagedBlob> stepBlobs = new List<ManagedBlob>();

        TimeSpan processingTime = TimeSpan.Zero;
        Stopwatch processingTimeWatch = new Stopwatch();

        public TimeSpan ProcessingTime
        {
            get { return processingTime; }
        }

        public TimeSpan ImageProcessingTime
        {
            get { return ProcessingEngine.ProcessingTime; }
        }

        private List<Vector2> additionnalBlobsCursors = new List<Vector2>();

        public List<Vector2> AdditionnalBlobsCursors
        {
            get { return additionnalBlobsCursors; }
        }

        private List<BlobsTracker.TrackedBlob> additionnalBlobs = new List<BlobsTracker.TrackedBlob>();

        public List<BlobsTracker.TrackedBlob> AdditionnalBlobs
        {
            get { return additionnalBlobs; }
        }

        public void Process(byte[] depthFilteredFrame32, float minDepth, float maxDepth)
        {
            processingTimeWatch.Reset();
            processingTimeWatch.Start();
            try
            {
                ProcessingEngine.Process(depthFilteredFrame32, minDepth, maxDepth);

                int validScoringBlobs = 0;

                stepBlobs.Clear();

                for (int i = 0; i < scoringBlobs.Length; i++)
                {
                    var i_blob = ProcessingEngine.MainBlobs[i];

                    if (i_blob == null || i_blob.PixelCount <= 10 * 10)
                        break;
                    else
                    {
                        validScoringBlobs++;
                        //scoringBlobs[i].MBlob = i_blob;

                        ReScale(i_blob);

                        stepBlobs.Add(i_blob);
                    }
                }

                BlobsTracker.Update(stepBlobs);

                UpdateCursors();
            }
            finally
            {
                processingTimeWatch.Stop();
                processingTime = processingTimeWatch.Elapsed;
            }
        }

        public static double ReScale(double v, double range)
        {
            var normalized_v = v / range;
            
            var directed_v = (1 / (1 + Math.Exp(normalized_v * 8 - 4))) * 1.05 - 0.025;
            directed_v = directed_v < 0 ? 0 : (directed_v > 1 ? 1 : directed_v);

            return (1 - directed_v) * range;

//#warning vérifier qu'on ne clamp rien ici (avec la kinect ... je l'ai pas là et j'ai la flemme)
//            var normalized_v = v / range; //[0 : 1]
//            var c = ((normalized_v) - 0.5) * 2 + 0.5; //[-0.5 : 0.5] => [-1 : 1] => [-0.5 : 1.5]
//            var result = (Math.Sin((c < 0 ? 0 : (c > 1 ? 1 : c)) * MathHelper.PiOver2) * range);

//            return result;
        }

        private void ReScale(ManagedBlob i_blob)
        {
            i_blob.LinearEstimatedCursorX = i_blob.EstimatedCursorX;
            i_blob.LinearEstimatedCursorY = i_blob.EstimatedCursorY;
            i_blob.LinearInvertedEstimatedCursorX = i_blob.InvertedEstimatedCursorX;
            i_blob.LinearInvertedEstimatedCursorY = i_blob.InvertedEstimatedCursorY;

            i_blob.EstimatedCursorX = ReScale(i_blob.EstimatedCursorX, DataWidth);
            i_blob.EstimatedCursorY = ReScale(i_blob.EstimatedCursorY, DataHeight);

            i_blob.InvertedEstimatedCursorX = ReScale(i_blob.InvertedEstimatedCursorX, DataWidth);
            i_blob.InvertedEstimatedCursorY = ReScale(i_blob.InvertedEstimatedCursorY, DataHeight);

            i_blob.AvgCenterX = ReScale(i_blob.AvgCenterX, DataWidth);
            i_blob.AvgCenterY = ReScale(i_blob.AvgCenterY, DataHeight);
        }

        bool wasCrossed = false;

        private void UpdateCursors()
        {
            var validBlobs = BlobsTracker.TrackedBlobs;

            if (validBlobs.Count < 1)
            {
                /* Nothing */

                //LeftHandBlob.CursorPosition = idealLeftHandPosition;
                LeftHandBlob.empty = true;

                //RightHandBlob.CursorPosition = idealRightHandPosition;
                RightHandBlob.empty = true;
            }

            CheckHandBlob(validBlobs, RightHandBlob, LeftHandBlob.MBlob, ref idealRightHandPosition);
            CheckHandBlob(validBlobs, LeftHandBlob, RightHandBlob.MBlob, ref idealLeftHandPosition);

            if (RightHandBlob.MBlob != null && LeftHandBlob.MBlob != null)
            {
                if (RightHandBlob.MBlob.Current.Crossed && LeftHandBlob.MBlob.Current.Crossed)
                {
                    var toTheLeft = RightHandBlob.MBlob.Current.EstimatedCursorX > LeftHandBlob.MBlob.Current.EstimatedCursorX
                        ? RightHandBlob.MBlob
                        : LeftHandBlob.MBlob;

                    var toTheRight = RightHandBlob.MBlob == toTheLeft ? LeftHandBlob.MBlob : RightHandBlob.MBlob;

                    RightHandBlob.MBlob = toTheRight;
                    LeftHandBlob.MBlob = toTheLeft;

                    wasCrossed = true;
                }
                else if (wasCrossed)
                {
                    wasCrossed = false;

                    var toTheRight = RightHandBlob.MBlob.Current.EstimatedCursorX < LeftHandBlob.MBlob.Current.EstimatedCursorX
                        ? RightHandBlob.MBlob
                        : LeftHandBlob.MBlob;

                    var toTheLeft = RightHandBlob.MBlob == toTheRight ? LeftHandBlob.MBlob : RightHandBlob.MBlob;

                    RightHandBlob.MBlob = toTheLeft;
                    LeftHandBlob.MBlob = toTheRight;
                }
            }

            additionnalBlobsCursors.Clear();
            additionnalBlobs.Clear();

#warning always update the list
            //if (RightHandBlob.MBlob == null || LeftHandBlob.MBlob == null)
            {
                /*
                 * This should help the user to see something at the screen
                 * when the cursors are not currently tracked
                 */
                foreach (var blob in validBlobs)
                {
                    if (blob != RightHandBlob.MBlob && blob != LeftHandBlob.MBlob)
                    {
                        additionnalBlobs.Add(blob);
                        additionnalBlobsCursors.Add(new Vector2((float)(blob.Current.EstimatedCursorX / DataWidth), (float)(blob.Current.EstimatedCursorY / DataHeight)));
                    }
                }
            }
        }

        private void CheckHandBlob(List<BlobsTracker.TrackedBlob> blobs, BlobParametersRecord handBlob, BlobsTracker.TrackedBlob excludedBlob, ref Vector2 idealHandPosition)
        {
            if (!blobs.Contains(handBlob.MBlob) || handBlob.MBlob == excludedBlob || handBlob.MBlob == null)
            {
                var p_idealPosition = new Vector2(idealHandPosition.X * DataWidth, idealHandPosition.Y * DataHeight);

                BlobsTracker.TrackedBlob closestBlob = null;
                double score = double.MaxValue;

                foreach (var item in blobs)
                {
                    if (item != excludedBlob)
                    {
                        var d = (new Vector2((float)item.Current.EstimatedCursorX, (float)item.Current.EstimatedCursorY) - p_idealPosition).Length();

                        if (d < score)
                        {
                            score = d;
                            closestBlob = item;
                        }
                    }
                }

                if (score < 20)
                {
                    handBlob.MBlob = closestBlob;
                    handBlob.empty = false;
                }
                else
                {
                    handBlob.MBlob = null;
                    handBlob.empty = true;
                }
            }

            if (handBlob.MBlob != null)
            {
                var blobCursor = handBlob.MBlob.InvertedCursor
                ? new Vector2((float)(handBlob.MBlob.Current.InvertedEstimatedCursorX / DataWidth), (float)(handBlob.MBlob.Current.InvertedEstimatedCursorY / DataHeight))
                : new Vector2((float)(handBlob.MBlob.Current.EstimatedCursorX / DataWidth), (float)(handBlob.MBlob.Current.EstimatedCursorY / DataHeight));

                var newPos = handBlob.CursorPosition * updateLatency + (1 - updateLatency) * blobCursor;

                if (!double.IsNaN(newPos.X) && !double.IsNaN(newPos.Y))
                    handBlob.CursorPosition = newPos;
            }
            else
                handBlob.CursorPosition = idealHandPosition;
        }

        private double ComputeScore(ref ManagedBlob newblob, ref ManagedBlob oldBlob, bool isEmptyOldBlob, bool considerInvertedCursor, ref Vector2 idealHandPosition)
        {
            /*
             * Le score dépend de:
             * 
             * - Distance par rapport à la position idéale de la main
             * - Distance par rapport à la position de l'ancien blob associé
             * - Taille par rapport à l'ancienne taille
             * - Orientation par rapport à l'ancienne orientation
             * */

            if (considerInvertedCursor && isEmptyOldBlob)
                return 0;

            var oldCenter = considerInvertedCursor
                ? new Vector2((float)(oldBlob.InvertedEstimatedCursorX / DataWidth), (float)(oldBlob.InvertedEstimatedCursorY / DataHeight))
                : new Vector2((float)(oldBlob.EstimatedCursorX / DataWidth), (float)(oldBlob.EstimatedCursorY / DataHeight));
            var newCenter = considerInvertedCursor
                ? new Vector2((float)(newblob.InvertedEstimatedCursorX / DataWidth), (float)(newblob.InvertedEstimatedCursorY / DataHeight))
                : new Vector2((float)(newblob.EstimatedCursorX / DataWidth), (float)(newblob.EstimatedCursorY / DataHeight));

            var oldSize = (float)oldBlob.PixelCount / (DataWidth * DataHeight);
            var newSize = (float)newblob.PixelCount / (DataWidth * DataHeight);

            float idealPositionScore;
            Vector2.Distance(ref newCenter, ref idealHandPosition, out idealPositionScore);

            var distanceScore = Math.Min(1, 0.1 / Math.Max(0.001, Vector2.Distance(oldCenter, newCenter)));
            var sizeScore = Math.Min(1, Math.Abs(newSize - oldSize) * 5);
            var orientationScore = (Math.Abs(oldBlob.AverageDirection - newblob.AverageDirection)) / Math.PI;

            //if (isEmptyOldBlob)
            //    return idealPositionScore * 0.1 + distanceScore * 0.75;
            //else
            {
                return
                    idealPositionScore * 10 +
                    distanceScore * 85 +
                    sizeScore * 1 +
                    orientationScore * 0.5;
            }
        }
    }
}
