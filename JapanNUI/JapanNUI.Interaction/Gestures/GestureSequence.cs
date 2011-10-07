using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction.Gestures
{
    /// <summary>
    /// Special queue-like structure to hold gesture sequences
    /// </summary>
    public class GestureSequence
    {
        private List<GestureSequenceKey> data = new List<GestureSequenceKey>();

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

        public GestureSequenceKey this[int index]
        {
            get
            {
                return data[data.Count - index - 1];
            }
        }

        public bool Enqueue(GestureSequenceKey key)
        {
            bool shouldAdd = true;

            /* ignore repetitions */
            if (data.Count > 0)
            {
                if (data[data.Count - 1].SameGestureAs(key))
                {
                    shouldAdd = false;
                    data[data.Count - 1] = new GestureSequenceKey() { simpleGestures = data[data.Count - 1].simpleGestures, keyTime = DateTime.Now };
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
                if ((refTime - data[0].keyTime) > MaxDuration)
                    data.RemoveAt(0);
                else
                    break;
            }
        }

        public override string ToString()
        {
            string res = "[";

            for (int i = 0; i < data.Count; i++)
            {
                res += "(";

                var seq = data[i];
                for(int j = 0;j<seq.simpleGestures.Length;j++)
                {
                    res += String.Format("\"{0}-{1}\"", seq.simpleGestures[j].ManagerId, seq.simpleGestures[j].MainGesture);

                    if (j + 1 < seq.simpleGestures.Length)
                        res += ",";
                }

                res += ")";

                if (i + 1 < data.Count)
                    res += " ; ";
            }

            return res + "]";
        }

        public void Reset()
        {
            data.Clear();
        }
    }
}
