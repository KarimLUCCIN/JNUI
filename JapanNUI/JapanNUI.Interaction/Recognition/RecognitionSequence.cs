using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Gestures;

namespace JapanNUI.Interaction.Recognition
{
    public class RecognitionSequence : List<SimpleGesture>
    {
        public RecognitionSequence()
        {

        }

        public RecognitionSequence(IEnumerable<SimpleGesture> content)
        {
            AddRange(content);
        }
    }
}
