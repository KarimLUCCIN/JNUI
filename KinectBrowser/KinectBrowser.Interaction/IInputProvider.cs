using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectBrowser.Interaction
{
    public interface IInputProvider
    {
        int Priority { get; }

        bool Available { get; }
        bool Enabled { get; set; }

        IPositionProvider[] Positions { get; }

        void Shutdown();

        void Update();

        TimeSpan ProcessingTime { get; }
    }
}
