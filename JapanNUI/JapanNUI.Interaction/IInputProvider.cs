using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction
{
    public interface IInputProvider
    {
        int Priority { get; }

        bool Available { get; }
        bool Enabled { get; set; }

        IPositionProvider[] Positions { get; }

        void Shutdown();
    }
}
