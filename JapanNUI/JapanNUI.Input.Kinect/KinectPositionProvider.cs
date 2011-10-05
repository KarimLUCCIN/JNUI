using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Input.Kinect
{
    public class KinectPositionProvider : IPositionProvider
    {
        public KinectProvider KinectProvider { get; private set; }

        public KinectPositionProvider(KinectProvider kinectProvider)
        {
            KinectProvider = kinectProvider;

            Position = Vector3.Zero;
            Velocity = Vector3.Zero;
            Acceleration = Vector3.Zero;
        }

        #region IPositionProvider Members

        public Vector3 Position { get; private set; }

        public Vector3 Velocity { get; private set; }

        public Vector3 Acceleration { get; private set; }

        #endregion

        bool updating = false;
        object sync = new object();

        public void Update(Vector3 skeletonPosition)
        {
            lock (sync)
            {
                if (updating)
                    return;

                updating = true;
            }
            try
            {
                var input = KinectProvider.Listener;

                var clientMousePos = input.ClientArea.RelativePointToAbsolutePoint(skeletonPosition.XY);

                var newPosition = 0.9 * Position + 0.1 * new Vector3(clientMousePos - input.ClientArea.Origin, skeletonPosition.Z);

                var newVelocity = newPosition - Position;

                Position = newPosition;

                var newAcceleration = newVelocity - Velocity;

                Velocity = newVelocity;
                Acceleration = newAcceleration;
            }
            finally
            {
                lock (sync)
                {
                    updating = false;
                }
            }
        }
    }
}
