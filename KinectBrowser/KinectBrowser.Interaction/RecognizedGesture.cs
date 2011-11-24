using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Gestures;
using KinectBrowser.Interaction.Recognition;

namespace KinectBrowser.Interaction
{
    public class RecognizedGesture
    {
        public RecognizedGesture(Dictionary<string, SimpleGesture[]> gestures)
        {
            if (gestures == null || gestures.Count < 1)
                throw new ArgumentNullException("gestures");

            Gesture = gestures.ToArray();

            Machines = (from g in Gesture select new KeyValuePair<string, RecognitionSequenceMachine>(g.Key, new RecognitionSequenceMachine(g.Value))).ToArray();
        }

        /// <summary>
        /// Gestures using
        /// key: provider key (ie. left or right)
        /// value: gesture sequence
        /// </summary>
        public KeyValuePair<string, SimpleGesture[]>[] Gesture { get; internal set; }

        /// <summary>
        /// Gestures using
        /// key: provider key (ie. left or right)
        /// value: recognition machine
        /// </summary>
        internal KeyValuePair<string, RecognitionSequenceMachine>[] Machines { get; set; }

        internal double LastScore { get; set; }

        public event EventHandler Activated;

        internal void RaiseActivated()
        {
            if (Activated != null)
                Activated(this, EventArgs.Empty);
        }
    }
}
