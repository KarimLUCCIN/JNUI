using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;
using JapanNUI.Interaction.Maths;
using JapanNUI.Input.Mouse.Interop;

namespace JapanNUI.Input.Mouse
{
    public class MousePositionProvider : IPositionProvider
    {
        public MouseProvider MouseProvider { get; private set; }

        public MousePositionProvider(MouseProvider mouseProvider)
        {
            MouseProvider = mouseProvider;

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

        public void Update()
        {
            lock (sync)
            {
                if (updating)
                    return;

                updating = true;
            }
            try
            {
                var mousePos = NativeFunctions.GetCursorPos();

                var input = MouseProvider.Listener;

                var clientMousePos = Vector2.Clamp(mousePos, input.ClientArea.Origin, input.ClientArea.Origin + input.ClientArea.Size);

                var newPosition = new Vector3(clientMousePos - input.ClientArea.Origin, 0);

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
