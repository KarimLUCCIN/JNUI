using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace JapanNUI.ImageProcessing
{
    public class ContourBuilder
    {
        private float minimumPointDistance = 5;

        public float MinimumPointDistance
        {
            get { return minimumPointDistance; }
            set { minimumPointDistance = value; }
        }

        private float maximumPointDistance = 15;

        public float MaximumPointDistance
        {
            get { return maximumPointDistance; }
            set { maximumPointDistance = value; }
        }
        
        
        public ContourBuilder()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bordersData">32bits floating point data</param>
        /// <param name="widht"></param>
        /// <param name="height"></param>
        public List<List<Vector2>> Process(byte[] bordersData, int width, int height)
        {
            var contours = new List<List<Vector2>>();

            for (int line = 0; line < height; line++)
            {
                for (int col = 0; col < width; col++)
                {
                    var isContour = VectorUtils.FloatFromBytes(bordersData, line * (width * 4) + col * 4) > 0;
                    var currentPoint = new Vector2(col, line);

                    if (isContour)
                    {
                        /*
                         * On cherche le point le plus proche qui soit un point de tête ou de queue de contour
                         * qui respecte maxmumPointDistance et minimumPointDistance. Si on en trouve pas,
                         * on crée un nouveau contour
                         * 
                         * */

                        List<Vector2> closestContour = null;
                        int closestContourInsertionIndex = -1;
                        float closestContourDistance = float.MaxValue;

                        for (int contourTest = 0; contourTest < contours.Count; contourTest++)
                        {
                            var current = contours[contourTest];

                            /* tête */
                            var currentDistance = Vector2.Distance(currentPoint, current[0]);

                            if (currentDistance > minimumPointDistance && currentDistance < maximumPointDistance && currentDistance < closestContourDistance)
                            {
                                closestContour = current;
                                closestContourInsertionIndex = 0;
                                closestContourDistance = currentDistance;
                            }

                            /* queue */
                            var current_count = current.Count;
                            if (current_count > 0)
                            {
                                currentDistance = Vector2.Distance(currentPoint, current[current_count - 1]);

                                if (currentDistance > minimumPointDistance && currentDistance < maximumPointDistance && currentDistance < closestContourDistance)
                                {
                                    closestContour = current;
                                    closestContourInsertionIndex = current_count;
                                    closestContourDistance = currentDistance;
                                }
                            }
                        }

                        if (closestContourInsertionIndex >= 0)
                        {
                            /* trouvé */
                            closestContour.Insert(closestContourInsertionIndex, currentPoint);
                        }
                        else
                        {
                            /* pas trouvé, début d'un nouveau contour */
                            contours.Add(new List<Vector2>(new Vector2[] { currentPoint }));
                        }
                    }
                }
            }

            return contours;
        }
    }
}
