using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Maths;
using JapanNUI.Interaction.Recognition;

namespace JapanNUI.Interaction
{
    public interface IPositionProvider
    {
        GesturePoint CurrentPoint { get; }
    }
}
