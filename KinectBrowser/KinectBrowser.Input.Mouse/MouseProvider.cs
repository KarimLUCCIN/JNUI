using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using KinectBrowser.Interaction;

namespace KinectBrowser.Input.Mouse
{
    public class MouseProvider : IInputProvider
    {
        private IPositionProvider[] providers;
        private MousePositionProvider provider;

        public IInputClient Client { get; private set; }

        public MouseProvider(IInputClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            Client = client;
            Enabled = true;

            provider = new MousePositionProvider("left", this);
            providers = new IPositionProvider[] { provider };
        }

        public bool Available
        {
            get { return true; }
        }

        #region IInputProvider Members

        public bool Enabled { get; set; }

        public IPositionProvider[] Positions
        {
            get { return providers; }
        }

        public void Shutdown()
        {

        }

        #endregion

        #region IInputProvider Members

        public int Priority
        {
            get { return 1; }
        }

        #endregion

        #region IInputProvider Members
        
        public void Update()
        {
            provider.Update();
        }

        #endregion

        #region IInputProvider Members
        
        public TimeSpan ProcessingTime
        {
            get { return TimeSpan.Zero; }
        }

        #endregion
    }
}
