using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;
using JapanNUI.Interaction.Maths;
using JapanNUI.Input.Mouse.Interop;
using JapanNUI.Interaction.Recognition;

namespace JapanNUI.Input.Mouse
{
    public class MousePositionProvider : BasePositionProvider
    {
        public MouseProvider MouseProvider { get; private set; }

        public MousePositionProvider(MouseProvider mouseProvider)
        {
            MouseProvider = mouseProvider;
            CurrentPoint.Latency = 1;
        }

        public void Update()
        {
            if (BeginUpdate())
            {
                try
                {
                    var mousePos = NativeFunctions.GetCursorPos();

                    var input = MouseProvider.Listener;

                    var clientMousePos = Vector2.Clamp(mousePos, input.ClientArea.Origin, input.ClientArea.Origin + input.ClientArea.Size);

                    CurrentPoint.UpdatePosition(new Vector3(clientMousePos - input.ClientArea.Origin, 0));
                }
                finally
                {
                    EndUpdate();
                }
            }
        }
    }
}
