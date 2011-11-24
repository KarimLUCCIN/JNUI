using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Maths;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Interaction.Gestures
{
    public class GesturePoint
    {
        public CursorState State { get; private set; }

        public GesturePoint()
        {
            Position = Vector3.Zero;
            Velocity = Vector3.Zero;
            Acceleration = Vector3.Zero;
            State = CursorState.Default;
        }

        private int pixelMoveTreshold = 0;

        public int PixelMoveTreshold
        {
            get { return pixelMoveTreshold; }
            set { pixelMoveTreshold = value; }
        }
        
        private Vector3 position = Vector3.Zero;

        public Vector3 Position
        {
            get { return position; }
            private set { position = value; }
        }

        public Vector3 Velocity { get; private set; }

        public Vector3 Acceleration { get; private set; }

        private int historySize = 1;

        public int HistorySize
        {
            get { return historySize; }
            set { historySize = Math.Max(1, value); }
        }

        private List<Vector3> positions = new List<Vector3>();

        private void EnqueuePosition(ref Vector3 position, out Vector3 medPosition)
        {
            positions.Insert(0, position);

            while (positions.Count > historySize)
                positions.RemoveAt(positions.Count - 1);

            Vector3 res = Vector3.Zero;

            for (int i = 0; i < positions.Count; i++)
                res += positions[i];

            medPosition = res * (1.0f / (float)positions.Count);
        }
        
        public void UpdatePosition(Vector3 newPosition, CursorState state)
        {
            State = state;

            Vector3 medPosition;
            EnqueuePosition(ref newPosition, out medPosition);

            if (pixelMoveTreshold > 0 && Vector3.Distance(medPosition, position) < pixelMoveTreshold)
                medPosition = position;
            else
                positions.Clear();

            var newVelocity = medPosition - Position;

            Position = medPosition;

            var newAcceleration = newVelocity - Velocity;

            Velocity = newVelocity;
            Acceleration = newAcceleration;
        }

        public GesturePoint Clone()
        {
            var pt = new GesturePoint();
            pt.CopyFrom(this);

            return pt;
        }

        public void CopyFrom(GesturePoint currentPoint)
        {
            if (currentPoint != null)
            {
                HistorySize = currentPoint.HistorySize;
                PixelMoveTreshold = currentPoint.PixelMoveTreshold;

                Position = currentPoint.Position;
                Velocity = currentPoint.Velocity;
                Acceleration = currentPoint.Acceleration;

                State = currentPoint.State;
            }
        }
    }
}
