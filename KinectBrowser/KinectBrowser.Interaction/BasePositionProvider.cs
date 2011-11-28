using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Gestures;

namespace KinectBrowser.Interaction
{
    public abstract class BasePositionProvider : IPositionProvider
    {
        public BasePositionProvider(string id)
        {
            if (String.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            Id = id;
            CurrentPoint = new GesturePoint();
        }

        #region IPositionProvider Members

        public GesturePoint CurrentPoint { get; private set; }

        public string Id { get; private set; }

        #endregion

        bool updating = false;
        object sync = new object();

        protected bool Updating
        {
            get { return updating; }
        }

        protected bool BeginUpdate()
        {
            lock (sync)
            {
                if (updating)
                    return false;
                else
                {
                    updating = true;
                    return true;
                }
            }
        }

        protected void EndUpdate()
        {
            lock (sync)
            {
                if (!updating)
                    throw new InvalidOperationException("A previous successfull BeginUpdate must be executed before calling EndUpdate");

                updating = false;
            }
        }

        #region IPositionProvider Members

        public bool LeftButtonCliked { get; protected set; }

        public bool RightButtonClicked { get; protected set; }

        #endregion
    }
}
