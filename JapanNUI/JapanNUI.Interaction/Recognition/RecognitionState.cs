using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction.Recognition
{
    public class RecognitionState
    {
        private int currentState;

        public int CurrentState
        {
            get { return currentState; }
            set { currentState = value; }
        }

        private double currentScore;

        public double CurrentScore
        {
            get { return currentScore; }
            set { currentScore = value; }
        }
    }
}
