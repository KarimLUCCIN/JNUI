using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Maths;
using JapanNUI.Interaction.Gestures;

namespace JapanNUI.Interaction
{
    public interface IPositionProvider
    {
        GesturePoint CurrentPoint { get; }

        string Id { get; }
    }
}
