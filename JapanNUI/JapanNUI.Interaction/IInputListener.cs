using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Interaction
{
    public interface IInputListener
    {
        Rectangle ScreenArea { get; }

        Rectangle ClientArea { get; }

        void Update(IInputProvider provider);
    }
}
