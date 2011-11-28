using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction;
using KinectBrowser.Input.Mouse.Interop;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Input.Mouse
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

                    var client = MouseProvider.Client;

                    var rectOrigin = new Vector2(client.ClientArea.X, client.ClientArea.Y);
                    var rectSize = new Vector2(client.ClientArea.Width, client.ClientArea.Height);

                    var clientMousePos = Vector2.Clamp(mousePos, rectOrigin, rectOrigin + rectSize);

                    CurrentPoint.UpdatePosition(new Vector3(clientMousePos - rectOrigin, 0), CursorState.Tracked);

                    LeftButtonClicked = NativeFunctions.IsLeftButtonPressed();
                    RightButtonClicked = NativeFunctions.IsRightButtonPressed();
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
