﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using KinectBrowser.Interaction.Maths;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Interaction.Gestures
{
    public class GestureManager
    {
        private List<KeyValuePair<SimpleGesture, Vector3>> gestureDirections = new List<KeyValuePair<SimpleGesture, Vector3>>();
        private Dictionary<SimpleGesture, double> currentGestureWeights = new Dictionary<SimpleGesture, double>();

        private SimpleGesture currentGesture;

        public SimpleGesture CurrentGesture
        {
            get { return currentGesture; }
            set { currentGesture = value; }
        }

        public WeightedSimpleGestureKey WeightedCurrentGesture
        {
            get
            {
                var result = new WeightedSimpleGestureKey(Id);

                result.WeightedSimpleGestures = (
                    from entry in currentGestureWeights 
                    select new WeightedSimpleGesture() { weight = entry.Value, gesture = entry.Key }).ToArray();

                return result;
            }
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

        private string id;

        public string Id
        {
            get { return id; }
            private set { id = value; }
        }
                
        public GestureManager(string id)
            :this(id, TimeSpan.FromSeconds(1 / 24.0))
        {

        }

        public GestureManager(string id, TimeSpan latency)
        {
            this.id = id;
            this.latency = latency;

            InitializeSimpleGesturesPatterns();
        }

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

        private bool idle;

        public bool Idle
        {
            get { return idle; }
            private set { idle = value; }
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
                {
                    currentGesture = selectedGesture;

                    /* weights */
                    foreach (var item in gestureDirections)
                    {
                        currentGestureWeights[item.Key] /= maxLength;
                    }
                    
                    idle = false;
                }
                else
                    idle = true;
            }
            else
                idle = true;
        }
    }
}