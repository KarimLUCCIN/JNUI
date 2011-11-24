using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectBrowser.Interaction
{
    public enum CursorState
    {
        /// <summary>
        /// The cursor is not currently tracked, and there's no informations about it
        /// </summary>
        Default,

        /// <summary>
        /// The cursor is currently tracking the user
        /// </summary>
        Tracked,

        /// <summary>
        /// The cursor was tracking the user, but has lost its target and is waiting for input
        /// </summary>
        StandBy
    }
}
