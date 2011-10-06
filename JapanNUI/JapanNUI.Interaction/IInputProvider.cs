using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction
{
    public interface IInputProvider
    {
        bool Available { get; }
        bool Enabled { get; }

        IPositionProvider[] Positions { get; }

        void Shutdown();
    }
}
