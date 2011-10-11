using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;

namespace JapanNUI.Input.Kinect
{
    public class KinectProviderBuilder : IInputProviderBuilder
    {
        #region IInputProviderBuilder Members

        public bool Create(IInputListener listener, out IInputProvider provider)
        {
            try
            {
                provider = new KinectProvider(listener);
                return provider.Available;
            }
            catch
            {
                provider = null;
                return false;
            }
        }

        #endregion
    }
}
