using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Gestures;

namespace JapanNUI.Interaction
{
    public abstract class BasePositionProvider : IPositionProvider
    {
        public BasePositionProvider(string id)
        {
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
    }
}
