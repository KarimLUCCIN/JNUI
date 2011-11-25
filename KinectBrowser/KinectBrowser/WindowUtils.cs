using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace KinectBrowser
{
    public static class WindowUtils
    {
        public static Microsoft.Xna.Framework.Rectangle GetScreenArea(MainWindow window)
        {
            var screen = System.Windows.Forms.Screen.FromHandle(window.WindowHandle);
            var screenBounds = screen.Bounds;

            return new Microsoft.Xna.Framework.Rectangle(screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);
        }
    }
}
