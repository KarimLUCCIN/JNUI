using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Interaction.Recognition
{
    public class GestureManager
    {
        private SimpleGesture currentGesture;

        public SimpleGesture CurrentGesture
        {
            get { return currentGesture; }
            set { currentGesture = value; }
        }
        
        private GesturePoint lastPoint = null;

        public GesturePoint LastPoint
        {
            get { return lastPoint; }
            private set { lastPoint = value; }
        }

        private TimeSpan latency;

        public TimeSpan Latency
        {
            get { return latency; }
            private set { latency = value; }
        }

        private double minimumGestureLength = 0.5;

        public double MinimumGestureLength
        {
            get { return minimumGestureLength; }
            set { minimumGestureLength = value; }
        }
        
        public GestureManager()
            :this(TimeSpan.FromSeconds(1 / 16.0))
        {

        }

        public GestureManager(TimeSpan latency)
        {
            this.latency = latency;

            InitializeSimpleGesturesPatterns();
        }

        List<KeyValuePair<SimpleGesture, Vector3>> gestureDirections = new List<KeyValuePair<SimpleGesture, Vector3>>();
        Dictionary<SimpleGesture, double> currentGestureWeights = new Dictionary<SimpleGesture, double>();

        private void InitializeSimpleGesturesPatterns()
        {
            gestureDirections.Add(new KeyValuePair<SimpleGesture, Vector3>(SimpleGesture.Bottom,
                Vector3.Normalize(new Vector3(0, 1, 0))));
            gestureDirections.Add(new KeyValuePair<SimpleGesture, Vector3>(SimpleGesture.BottomLeft,
                Vector3.Normalize(new Vector3(-1, 1, 0))));
            gestureDirections.Add(new KeyValuePair<SimpleGesture, Vector3>(SimpleGesture.BottomRight,
                Vector3.Normalize(new Vector3(1, 1, 0))));
            gestureDirections.Add(new KeyValuePair<SimpleGesture, Vector3>(SimpleGesture.Left,
                Vector3.Normalize(new Vector3(-1, 0, 0))));
            gestureDirections.Add(new KeyValuePair<SimpleGesture, Vector3>(SimpleGesture.Right,
                Vector3.Normalize(new Vector3(1, 0, 0))));
            gestureDirections.Add(new KeyValuePair<SimpleGesture, Vector3>(SimpleGesture.Top,
                Vector3.Normalize(new Vector3(0, -1, 0))));
            gestureDirections.Add(new KeyValuePair<SimpleGesture, Vector3>(SimpleGesture.TopLeft,
                Vector3.Normalize(new Vector3(-1, -1, 0))));
            gestureDirections.Add(new KeyValuePair<SimpleGesture, Vector3>(SimpleGesture.TopRight,
                Vector3.Normalize(new Vector3(1, -1, 0))));

            foreach (var gesture in gestureDirections)
                currentGestureWeights[gesture.Key] = 0;
        }

        public void Reset()
        {
            lastPoint = null;
            updateTimer.Stop();
            currentGesture = SimpleGesture.None;
        }

        Stopwatch updateTimer = new Stopwatch();

        public void Update(GesturePoint currentPoint)
        {
            if (lastPoint == null)
            {
                lastPoint = new GesturePoint();
                lastPoint.CopyFrom(currentPoint);

                updateTimer.Reset();
                updateTimer.Start();
            }
            else
            {
                if (updateTimer.Elapsed > latency)
                {
                    var vector = currentPoint.Position - lastPoint.Position;
                    lastPoint.CopyFrom(currentPoint);

                    ProcessGestureVector(vector);

                    updateTimer.Restart();
                }
            }
        }

        private void ProcessGestureVector(Vector3 vector)
        {
            if (vector.Length() > minimumGestureLength)
            {
                double maxLength = 0;
                SimpleGesture selectedGesture = SimpleGesture.None;

                /* main direction */
                foreach (var item in gestureDirections)
                {
                    var d = Vector3.Dot(vector, item.Value);
                    currentGestureWeights[item.Key] = Math.Max(0, d);

                    if (d > minimumGestureLength && d > maxLength)
                    {
                        maxLength = d;
                        selectedGesture = item.Key;
                    }
                }

                if (selectedGesture != SimpleGesture.None)
                    currentGesture = selectedGesture;

                /* weights */
                foreach (var item in gestureDirections)
                {
                    currentGestureWeights[item.Key] /= maxLength;
                }
            }
        }
    }
}
