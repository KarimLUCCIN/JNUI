using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace JapanNUI
{
    public static class WindowUtils
    {
        public static Interaction.Maths.Rectangle GetScreenArea(MainWindow window)
        {
            var screen = System.Windows.Forms.Screen.FromHandle(window.WindowHandle);
            var screenBounds = screen.Bounds;

            return new Interaction.Maths.Rectangle(screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);
        }
    }
}
