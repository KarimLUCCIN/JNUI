﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Input.Mouse.Interop
{
    internal static class NativeFunctions
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        private enum SystemMetric
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1,
            SM_CXVSCROLL = 2,
            SM_CYHSCROLL = 3,
            SM_CYCAPTION = 4,
            SM_CXBORDER = 5,
            SM_CYBORDER = 6,
            SM_CXDLGFRAME = 7,
            SM_CYDLGFRAME = 8,
            SM_CYVTHUMB = 9,
            SM_CXHTHUMB = 10,
            SM_CXICON = 11,
            SM_CYICON = 12,
            SM_CXCURSOR = 13,
            SM_CYCURSOR = 14,
            SM_CYMENU = 15,
            SM_CXFULLSCREEN = 16,
            SM_CYFULLSCREEN = 17,
            SM_CYKANJIWINDOW = 18,
            SM_MOUSEPRESENT = 19,
            SM_CYVSCROLL = 20,
            SM_CXHSCROLL = 21,
            SM_DEBUG = 22,
            SM_SWAPBUTTON = 23,
            SM_RESERVED1 = 24,
            SM_RESERVED2 = 25,
            SM_RESERVED3 = 26,
            SM_RESERVED4 = 27,
            SM_CXMIN = 28,
            SM_CYMIN = 29,
            SM_CXSIZE = 30,
            SM_CYSIZE = 31,
            SM_CXFRAME = 32,
            SM_CYFRAME = 33,
            SM_CXMINTRACK = 34,
            SM_CYMINTRACK = 35,
            SM_CXDOUBLECLK = 36,
            SM_CYDOUBLECLK = 37,
            SM_CXICONSPACING = 38,
            SM_CYICONSPACING = 39,
            SM_MENUDROPALIGNMENT = 40,
            SM_PENWINDOWS = 41,
            SM_DBCSENABLED = 42,
            SM_CMOUSEBUTTONS = 43,

            /*#if(WINVER >= 0x0400)*/
            SM_CXFIXEDFRAME = SM_CXDLGFRAME,  /* ;win40 name change */
            SM_CYFIXEDFRAME = SM_CYDLGFRAME, /* ;win40 name change */
            SM_CXSIZEFRAME = SM_CXFRAME,    /* ;win40 name change */
            SM_CYSIZEFRAME = SM_CYFRAME,     /* ;win40 name change */

            SM_SECURE = 44,
            SM_CXEDGE = 45,
            SM_CYEDGE = 46,
            SM_CXMINSPACING = 47,
            SM_CYMINSPACING = 48,
            SM_CXSMICON = 49,
            SM_CYSMICON = 50,
            SM_CYSMCAPTION = 51,
            SM_CXSMSIZE = 52,
            SM_CYSMSIZE = 53,
            SM_CXMENUSIZE = 54,
            SM_CYMENUSIZE = 55,
            SM_ARRANGE = 56,
            SM_CXMINIMIZED = 57,
            SM_CYMINIMIZED = 58,
            SM_CXMAXTRACK = 59,
            SM_CYMAXTRACK = 60,
            SM_CXMAXIMIZED = 61,
            SM_CYMAXIMIZED = 62,
            SM_NETWORK = 63,
            SM_CLEANBOOT = 67,
            SM_CXDRAG = 68,
            SM_CYDRAG = 69,
            /*#endif /* WINVER >= 0x0400 */
            SM_SHOWSOUNDS = 70,
            /*#if(WINVER >= 0x0400)*/
            SM_CXMENUCHECK = 71,   /* Use instead of GetMenuCheckMarkDimensions()! */
            SM_CYMENUCHECK = 72,
            SM_SLOWMACHINE = 73,
            SM_MIDEASTENABLED = 74,
            /*#endif /* WINVER >= 0x0400 */

            /*#if (WINVER >= 0x0500) || (_WIN32_WINNT >= 0x0400)*/
            SM_MOUSEWHEELPRESENT = 75,
            /*#endif*/
            /*#if(WINVER >= 0x0500)*/
            SM_XVIRTUALSCREEN = 76,
            SM_YVIRTUALSCREEN = 77,
            SM_CXVIRTUALSCREEN = 78,
            SM_CYVIRTUALSCREEN = 79,
            SM_CMONITORS = 80,
            SM_SAMEDISPLAYFORMAT = 81,
            /*#endif /* WINVER >= 0x0500 */
            /*#if(_WIN32_WINNT >= 0x0500)*/
            SM_IMMENABLED = 82,
            /*#endif /* _WIN32_WINNT >= 0x0500 */
            /*#if(_WIN32_WINNT >= 0x0501)*/
            SM_CXFOCUSBORDER = 83,
            SM_CYFOCUSBORDER = 84,
            /*#endif /* _WIN32_WINNT >= 0x0501 */

            /*#if(_WIN32_WINNT >= 0x0501)*/
            SM_TABLETPC = 86,
            SM_MEDIACENTER = 87,
            /*#endif /* _WIN32_WINNT >= 0x0501 */

            /*#if (WINVER < 0x0500) && (!defined(_WIN32_WINNT) || (_WIN32_WINNT < 0x0400))*/
            SM_CMETRICS_OTHER = 76,
            /*#elif WINVER == 0x500*/
            SM_CMETRICS_2000 = 83,
            /*#else*/
            SM_CMETRICS_NT = 88,
            /*#endif*/

            /*#if(WINVER >= 0x0500)*/
            SM_REMOTESESSION = 0x1000,

            /*#if(_WIN32_WINNT >= 0x0501)*/
            SM_SHUTTINGDOWN = 0x2000,
            /*#endif /* _WIN32_WINNT >= 0x0501 */

            /*#if(WINVER >= 0x0501)*/
            SM_REMOTECONTROL = 0x2001,
            /*#endif /* WINVER >= 0x0501 */

            /*#endif /* WINVER >= 0x0500 */
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(SystemMetric smIndex);

        public static bool IsLeftButtonPressed()
        {
            /* left & right switchés ou pas ? */
            bool switched = GetSystemMetrics(SystemMetric.SM_SWAPBUTTON) != 0;

            return (GetAsyncKeyState(switched ? System.Windows.Forms.Keys.RButton : System.Windows.Forms.Keys.LButton) & 0x8000) != 0;
        }

        public static bool IsRightButtonPressed()
        {
            /* left & right switchés ou pas ? */
            bool switched = GetSystemMetrics(SystemMetric.SM_SWAPBUTTON) != 0;

            return (GetAsyncKeyState(switched ? System.Windows.Forms.Keys.LButton : System.Windows.Forms.Keys.RButton) & 0x8000) != 0;
        }

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
