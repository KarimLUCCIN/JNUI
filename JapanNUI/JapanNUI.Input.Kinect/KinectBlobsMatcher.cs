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

            internal ManagedBlob MBlob = new ManagedBlob();

            internal double ScoreForLeftHand;
            internal double ScoreForRightHand;

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

        public void Process(byte[] depthFilteredFrame32, float minDepth, float maxDepth)
        {
            ProcessingEngine.Process(depthFilteredFrame32, minDepth, maxDepth);

            int validScoringBlobs = 0;

            for (int i = 0; i < scoringBlobs.Length; i++)
            {
                var i_blob = ProcessingEngine.MainBlobs[i];

                if (i_blob == null || i_blob.PixelCount <= 10 * 10)
                    break;
                else
                {
                    validScoringBlobs++;
                    scoringBlobs[i].MBlob = i_blob;
                }
            }

            if (validScoringBlobs < 1)
            {
                /* Nothing */

                //LeftHandBlob.CursorPosition = idealLeftHandPosition;
                LeftHandBlob.empty = true;
                
                //RightHandBlob.CursorPosition = idealRightHandPosition;
                RightHandBlob.empty = true;
            }
            else if (validScoringBlobs == 1)
            {
                /* Right hand has priority */
                //LeftHandBlob.CursorPosition = idealLeftHandPosition;
                LeftHandBlob.empty = true;

                RightHandBlob.CursorPosition = new Vector2(scoringBlobs[0].MBlob.EstimatedCursorX / DataWidth, scoringBlobs[0].MBlob.EstimatedCursorY / DataHeight);
                RightHandBlob.MBlob = scoringBlobs[0].MBlob;
                RightHandBlob.empty = false;
            }
            else
            {
                double maxLeftScore = 0;
                double maxRightScore = 0;

                leftScoringSort.Clear();
                rightScoringSort.Clear();

                for (int i = 0; i < validScoringBlobs; i++)
                {
                    var current = scoringBlobs[i];

                    current.assigned = false;
                    current.ScoreForLeftHand = ComputeScore(ref current.MBlob, ref LeftHandBlob.MBlob, LeftHandBlob.empty, ref idealLeftHandPosition);
                    current.ScoreForRightHand = ComputeScore(ref current.MBlob, ref RightHandBlob.MBlob, RightHandBlob.empty, ref idealRightHandPosition);

                    maxLeftScore = Math.Max(maxLeftScore, current.ScoreForLeftHand);
                    maxRightScore = Math.Max(maxRightScore, current.ScoreForRightHand);

                    leftScoringSort.Add(current);
                    rightScoringSort.Add(current);
                }

                SortScorings();

                BlobParametersRecord leftData;
                BlobParametersRecord rightData;

                if (maxLeftScore > maxRightScore)
                {
                    /* priority to the left */
                    leftData = leftScoringSort[0];
                    leftData.assigned = true;

                    /* now the right */
                    rightData = rightScoringSort[0].assigned ? rightScoringSort[1] : rightScoringSort[0];
                }
                else
                {
                    /* priority to the right */
                    rightData = rightScoringSort[0];
                    rightData.assigned = true;

                    /* now the left */
                    leftData = leftScoringSort[0].assigned ? leftScoringSort[1] : leftScoringSort[0];
                }

                if (leftData.MBlob.EstimatedCursorX > rightData.MBlob.EstimatedCursorX)
                {
                    /* exchange */
                    var tmp = leftData;
                    leftData = rightData;
                    rightData = tmp;
                }

                LeftHandBlob.MBlob = leftData.MBlob;
                LeftHandBlob.CursorPosition = new Vector2(leftData.MBlob.EstimatedCursorX / DataWidth, leftData.MBlob.EstimatedCursorY / DataHeight);

                RightHandBlob.MBlob = rightData.MBlob;
                RightHandBlob.CursorPosition = new Vector2(rightData.MBlob.EstimatedCursorX / DataWidth, rightData.MBlob.EstimatedCursorY / DataHeight);
            }
        }

        private void SortScorings()
        {
            leftScoringSort.Sort((a, b) => (-1) * a.ScoreForLeftHand.CompareTo(b.ScoreForLeftHand));
            rightScoringSort.Sort((a, b) => (-1) * a.ScoreForRightHand.CompareTo(b.ScoreForRightHand));
        }

        private double ComputeScore(ref ManagedBlob newblob, ref ManagedBlob oldBlob, bool isEmptyOldBlob, ref Vector2 idealHandPosition)
        {
            /*
             * Le score dépend de:
             * 
             * - Distance par rapport à la position idéale de la main
             * - Distance par rapport à la position de l'ancien blob associé
             * - Taille par rapport à l'ancienne taille
             * - Orientation par rapport à l'ancienne orientation
             * */

            var oldCenter = new Vector2(oldBlob.AvgCenterX / DataWidth, oldBlob.AvgCenterY / DataHeight);
            var newCenter = new Vector2(newblob.AvgCenterX / DataWidth, newblob.AvgCenterY / DataHeight);

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
