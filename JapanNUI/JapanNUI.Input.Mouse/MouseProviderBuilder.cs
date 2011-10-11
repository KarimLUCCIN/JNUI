using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;

namespace JapanNUI.Input.Mouse
{
    public class MouseProviderBuilder : IInputProviderBuilder
    {
        #region IInputProviderBuilder Members

        public bool Create(IInputListener listener, out IInputProvider provider)
        {
            provider = new MouseProvider(listener);
            return true;
        }

        #endregion
    }
}
