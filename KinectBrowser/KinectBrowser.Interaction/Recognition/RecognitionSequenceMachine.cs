using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Gestures;

namespace KinectBrowser.Interaction.Recognition
{
    public class RecognitionSequenceMachine
    {
        public RecognitionSequence Descriptor { get; private set; }

        public List<RecognitionState> CurrentStates { get; private set; }

        /// <summary>
        /// If the previous update has came to a final state, true. Else, false.
        /// </summary>
        public bool Valid { get; private set; }

        /// <summary>
        /// If the previous update has came to a final state, its score.
        /// </summary>
        public double Score{get;private set;}

        public RecognitionSequenceMachine(IEnumerable<SimpleGesture> gestures)
            :this(new RecognitionSequence(gestures))
        {

        }

        public RecognitionSequenceMachine(RecognitionSequence descriptor)
        {
            if (descriptor == null || descriptor.Count < 1)
                throw new ArgumentNullException("descriptor");

            Descriptor = descriptor;
            CurrentStates = new List<RecognitionState>();
        }

        public void Update(SimpleGesture gesture)
        {
            if (gesture != SimpleGesture.None)
            {
                double closeness;

                /* check for a progress in the previous states */
                for (int i = 0; i < CurrentStates.Count; i++)
                {
                    var state = CurrentStates[i];

                    closeness = SimpleGestureClosenessEvaluator.Compare(Descriptor[state.CurrentState], gesture);

                    if (closeness > 0)
                    {
                        /* next state */

                        if (closeness == 1)
                        {
                            /* full transition */
                            state.CurrentState++;
                            state.CurrentScore += closeness;
                        }
                        else
                        {
                            /* full transition + another instance staying in the same state */
                            state.CurrentState++;
                            state.CurrentScore += closeness;

                            CurrentStates.Insert(i, new RecognitionState() { CurrentScore = state.CurrentScore, CurrentState = state.CurrentState - 1 });

                            /* do not process it now */
                            i++;
                        }
                    }
                    else
                    {
                        /* not recognized */
                        CurrentStates.RemoveAt(i);
                        i--;
                    }
                }

                /* compare for a new entry in the state machine */
                closeness = SimpleGestureClosenessEvaluator.Compare(Descriptor[0], gesture);
                if (closeness > 0)
                {
                    var newState = new RecognitionState() { CurrentState = 1, CurrentScore = closeness };
                    CurrentStates.Add(newState);
                }

                /* check for validation */
                Valid = false;

                for (int i = 0; i < CurrentStates.Count; i++)
                {
                    var state = CurrentStates[i];

                    if (state.CurrentState >= Descriptor.Count)
                    {
                        Valid = true;
                        Score = state.CurrentScore;

                        CurrentStates.RemoveAt(i);
                        break;
                    }
                }

                if (Valid)
                {
                    /* sequence recognized, clear states */
                    CurrentStates.Clear();
                }
            }
        }

        public void Reset()
        {
            CurrentStates.Clear();
            Valid = false;
            Score = 0;
        }
    }
}
