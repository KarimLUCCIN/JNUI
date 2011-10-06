using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction.Gestures
{
    public struct GestureSequenceKey
    {
        public WeightedSimpleGestureKey[] simpleGestures;

        public DateTime keyTime;

        public bool SameGestureAs(GestureSequenceKey other)
        {
            if (simpleGestures == null || simpleGestures.Length <= 0)
                return other.simpleGestures == null || other.simpleGestures.Length <= 0;
            else
            {
                if (simpleGestures.Length != other.simpleGestures.Length)
                    return false;
                else
                {
                    for (int i = 0; i < simpleGestures.Length; i++)
                    {
                        if (simpleGestures[i].MainGesture != other.simpleGestures[i].MainGesture)
                            return false;
                    }

                    return true;
                }
            }
        }
    }
}
