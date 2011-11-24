using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectBrowser.Interaction.Gestures
{
    public class WeightedSimpleGestureKey
    {
        public string ManagerId { get; set; }

        public WeightedSimpleGestureKey(string managerId)
        {
            ManagerId = managerId;
        }

        public WeightedSimpleGesture[] WeightedSimpleGestures { get; set; }

        public SimpleGesture MainGesture
        {
            get
            {
                if (WeightedSimpleGestures == null)
                    return SimpleGesture.None;
                else
                {
                    var val = (from w in WeightedSimpleGestures where w.weight == 1 select w).FirstOrDefault();

                    if (val.weight != 1)
                        return SimpleGesture.None;
                    else
                        return val.gesture;
                }
            }
        }
    }
}
