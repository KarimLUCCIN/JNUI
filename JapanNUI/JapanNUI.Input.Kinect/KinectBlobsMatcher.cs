using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.ImageProcessing;
using JapanNUI.ImageProcessing.SectionsBuilders;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Input.Kinect
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

            internal bool useInvertedLeftCursor = false;
            internal bool useInvertedRightCursor = false;

            /// <summary>
            /// Position du curseur associé, entre (-1,-1) et (1,1)
            /// </summary>
            public Vector2 CursorPosition { get; set; }
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
        public KinectBlobsMatcher(ImageProcessingEngine processingEngine, int dataWidth,int dataHeight)
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

        public void Process(byte[] depthFilteredFrame32, float minDepth, float maxDepth)
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

                    stepBlobs.Add(i_blob);
                }
            }

            BlobsTracker.Update(stepBlobs);

            UpdateCursors();
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
                        var d = (new Vector2(item.Current.EstimatedCursorX, item.Current.EstimatedCursorY) - p_idealPosition).Length();

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
                var newPos = handBlob.CursorPosition * updateLatency + (1 - updateLatency) * new Vector2(handBlob.MBlob.Current.EstimatedCursorX / DataWidth, handBlob.MBlob.Current.EstimatedCursorY / DataHeight);

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
                ? new Vector2(oldBlob.InvertedEstimatedCursorX / DataWidth, oldBlob.InvertedEstimatedCursorY / DataHeight)
                : new Vector2(oldBlob.EstimatedCursorX / DataWidth, oldBlob.EstimatedCursorY / DataHeight);
            var newCenter = considerInvertedCursor 
                ? new Vector2(newblob.InvertedEstimatedCursorX / DataWidth, newblob.InvertedEstimatedCursorY / DataHeight)
                : new Vector2(newblob.EstimatedCursorX / DataWidth, newblob.EstimatedCursorY / DataHeight);

            var oldSize = (float)oldBlob.PixelCount / (DataWidth * DataHeight);
            var newSize = (float)newblob.PixelCount / (DataWidth * DataHeight);

            var idealPositionScore = Vector2.Distance(ref newCenter, ref idealHandPosition);
            var distanceScore = Math.Min(1, 0.1 / Math.Max(0.001, Vector2.Distance(ref oldCenter, ref newCenter)));
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
