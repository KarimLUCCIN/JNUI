using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;
using JapanNUI.Interaction.Maths;
using JapanNUI.Input.Mouse.Interop;

namespace JapanNUI.Input.Mouse
{
    public class MousePositionProvider : BasePositionProvider
    {
        public MouseProvider MouseProvider { get; private set; }

        public MousePositionProvider(string id, MouseProvider mouseProvider)
            :base(id)
        {
            MouseProvider = mouseProvider;
            CurrentPoint.HistorySize = 1;
        }

        public bool Update()
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

                return true;
            }
            else
                return false;
        }
    }
}
