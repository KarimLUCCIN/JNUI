using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction
{
    public interface IInputProviderBuilder
    {
        bool Create(IInputListener listener, out IInputProvider provider);
    }
}
