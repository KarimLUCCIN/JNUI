using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Interaction.Recognition
{
    public class GesturePoint
    {
        public GesturePoint()
        {
            Latency = 0.1;

            Position = Vector3.Zero;
            Velocity = Vector3.Zero;
            Acceleration = Vector3.Zero;
        }

        public Vector3 Position { get; private set; }

        public Vector3 Velocity { get; private set; }

        public Vector3 Acceleration { get; private set; }

        private double latency;

        public double Latency
        {
            get { return latency; }
            set { latency = value; }
        }
        
        public void UpdatePosition(Vector3 newPosition)
        {
            newPosition = latency * newPosition + (1 - latency) * Position;

            var newVelocity = newPosition - Position;

            Position = newPosition;

            var newAcceleration = newVelocity - Velocity;

            Velocity = newVelocity;
            Acceleration = newAcceleration;
        }

        public void CopyFrom(GesturePoint currentPoint)
        {
            if (currentPoint != null)
            {
                Latency = currentPoint.Latency;

                Position = currentPoint.Position;
                Velocity = currentPoint.Velocity;
                Acceleration = currentPoint.Acceleration;
            }
        }
    }
}
