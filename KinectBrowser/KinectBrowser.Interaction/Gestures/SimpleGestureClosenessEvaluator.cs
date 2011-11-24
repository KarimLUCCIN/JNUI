using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectBrowser.Interaction.Gestures
{
    public static class SimpleGestureClosenessEvaluator
    {
        private static Dictionary<SimpleGesture, double> gesturesAngle = new Dictionary<SimpleGesture, double>();

        static SimpleGestureClosenessEvaluator()
        {
            gesturesAngle[SimpleGesture.None] = 0;

            const double pi = Math.PI;
            const double piOverTwo = Math.PI / 2.0;
            const double piOverFour = Math.PI / 4.0;

            gesturesAngle[SimpleGesture.Bottom] = 3 * piOverTwo;
            gesturesAngle[SimpleGesture.BottomLeft] = pi + piOverFour;
            gesturesAngle[SimpleGesture.BottomRight] = 3 * piOverTwo + piOverFour;
            gesturesAngle[SimpleGesture.Left] = pi;
            gesturesAngle[SimpleGesture.Right] = 0;
            gesturesAngle[SimpleGesture.Top] = piOverTwo;
            gesturesAngle[SimpleGesture.TopLeft] = piOverTwo + piOverFour;
            gesturesAngle[SimpleGesture.TopRight] = piOverFour;
        }

        public static double Compare(SimpleGesture a, SimpleGesture b)
        {
            if (a == b)
                return 1;
            else if (a == SimpleGesture.None)
            {
                if (b != SimpleGesture.None)
                    return 0;
                else
                    return 1;
            }
            else if (b == SimpleGesture.None)
            {
                if (a != SimpleGesture.None)
                    return 0;
                else
                    return 1;
            }
            else
            {
                var aAngle = gesturesAngle[a];
                var bAngle = gesturesAngle[b];

                const double twoPi = 2 * Math.PI;

                var diffA = ((bAngle - aAngle) % (twoPi) + twoPi) % twoPi;
                var diffB = ((aAngle - bAngle) % (twoPi) + twoPi) % twoPi;

                var diff = Math.Min(diffA, diffB);

                if (Math.Abs(diff) <= Math.PI / 4.0 + Math.PI / 16.0)
                    return 0.5;
                else
                    return 0;
            }
        }
    }
}
