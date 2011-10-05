using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Interaction
{
    public interface IPositionProvider
    {
        Vector3 Position { get; }
        Vector3 Velocity { get; }
        Vector3 Acceleration { get; }
    }
}
