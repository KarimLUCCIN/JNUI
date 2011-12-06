using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectBrowser.Interaction.Gestures
{
    /// <summary>
    /// Special queue-like structure to hold gesture sequences
    /// </summary>
    public class GestureSequence
    {
        private List<SimpleGestureKey> data = new List<SimpleGestureKey>();

        /// <summary>
        /// Maximum amount of time that define an unique sequence
        /// </summary>
        public TimeSpan MaxDuration { get; private set; }

        public DateTime LastModificationTime { get; private set; }

        public GestureSequence(TimeSpan maxDuration)
        {
            MaxDuration = maxDuration;
            LastModificationTime = DateTime.MinValue;
        }

        public int Count
        {
            get { return data.Count; }
        }

        public SimpleGestureKey this[int index]
        {
            get
            {
                return data[data.Count - index - 1];
            }
        }

        public bool Enqueue(SimpleGestureKey key)
        {
            bool shouldAdd = true;

            /* ignore repetitions */
            if (data.Count > 0)
            {
                if (data[data.Count - 1].Gesture == key.Gesture)
                {
                    shouldAdd = false;
                    data[data.Count - 1].Time = key.Time;
                }
            }

            if (shouldAdd)
                data.Add(key);

            CleanOldValues();

            if (shouldAdd)
                LastModificationTime = DateTime.Now;

            return shouldAdd;
        }

        public void CleanOldValues()
        {
            var refTime = DateTime.Now;

            /* remove old values */
            while (data.Count > 0)
            {
                if ((refTime - data[0].Time) > MaxDuration)
                    data.RemoveAt(0);
                else
                    break;
            }
        }

        public override string ToString()
        {
            try
            {
                string res = "[";

                for (int i = 0; i < data.Count; i++)
                {
                    res += "(" + data[i].Gesture.ToString() + ")";

                    if (i + 1 < data.Count)
                        res += " ; ";
                }

                return res + "]";
            }
            catch
            {
                return "{invalid}";
            }
        }

        public void Reset()
        {
            data.Clear();
        }
    }
}
