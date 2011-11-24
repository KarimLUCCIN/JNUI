using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Maths;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Interaction.Gestures
{
    public class GestureSequenceManager
    {
        public GestureManager[] Managers { get; private set; }

        private double minimumGestureLength = 0.02;

        public double MinimumGestureLength
        {
            get { return minimumGestureLength; }
            set { minimumGestureLength = value; }
        }
        
        public GestureSequenceManager(GestureManager[] managers, TimeSpan maxGestureDuration)
        {
            if (managers == null || managers.Length < 1)
                throw new ArgumentNullException("managers");

            Managers = managers;
            CurrentSequence = new GestureSequence(maxGestureDuration);
        }

        public GestureSequence CurrentSequence { get; private set; }

        public void Update(IPositionProvider[] positions, Rectangle clientArea)
        {
            /* global scaling for gesture values */
            var areaFactor = Math.Min(clientArea.Width, clientArea.Height);
            var computedMinimumGestureLength = minimumGestureLength * areaFactor;

            /* update all managers and retrieve their current gesture */
            var currentSequenceKey = new GestureSequenceKey();
            currentSequenceKey.keyTime = DateTime.Now;
            currentSequenceKey.simpleGestures = new WeightedSimpleGestureKey[positions.Length];

            bool hasNonIdleContent = false;

            for (int i = 0; i < Managers.Length; i++)
            {
                var manager = Managers[i];
                var position = positions[i];

                manager.MinimumGestureLength = computedMinimumGestureLength;
                manager.Update(position.CurrentPoint);

                currentSequenceKey.simpleGestures[i] = manager.WeightedCurrentGesture;

                if (!manager.Idle)
                    hasNonIdleContent = true;
            }

            if (hasNonIdleContent)
                CurrentSequence.Enqueue(currentSequenceKey);
            else
                CurrentSequence.CleanOldValues();
        }
    }
}
