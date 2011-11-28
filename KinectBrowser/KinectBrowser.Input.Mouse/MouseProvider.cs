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

        public int Priority
        {
            get { return 1; }
        }

        public void Update()
        {
            provider.Update();
        }
        
        public TimeSpan ProcessingTime
        {
            get { return TimeSpan.Zero; }
        }

        public IPositionProvider MainPosition
        {
            get { return providers[0]; }
        }

        #endregion
    }
}
