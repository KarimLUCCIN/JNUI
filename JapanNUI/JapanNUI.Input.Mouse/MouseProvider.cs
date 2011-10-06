using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;
using System.Threading;

namespace JapanNUI.Input.Mouse
{
    public class MouseProvider : IInputProvider
    {
        private Timer actionsTimer;

        private IPositionProvider[] providers;
        private MousePositionProvider provider;

        public IInputListener Listener { get; private set; }

        public MouseProvider(IInputListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            Listener = listener;
            Enabled = true;

            provider = new MousePositionProvider(this);
            providers = new IPositionProvider[] { provider };

            actionsTimer = new Timer(delegate
                {
                    ThreadFunction();
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1 / 30.0));
        }

        public bool Available
        {
            get { return true; }
        }

        private void ThreadFunction()
        {
            if (Enabled)
            {
                provider.Update();
                Listener.Update(this);
            }
        }

        #region IInputProvider Members

        public bool Enabled { get; set; }

        public IPositionProvider[] Positions
        {
            get { return providers; }
        }

        public void Shutdown()
        {
            actionsTimer.Dispose();
        }

        #endregion
    }
}
