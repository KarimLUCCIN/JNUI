using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Maths;
using System.Runtime.InteropServices;

namespace JapanNUI.Input.Mouse.Interop
{
    internal static class NativeFunctions
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        public static Vector2 GetCursorPos()
        {
            POINT t;

            if (GetCursorPos(out t))
                return new Vector2(t.X, t.Y);
            else
                return Vector2.Zero;
        }
    }
}
