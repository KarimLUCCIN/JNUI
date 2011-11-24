using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectBrowser.Interaction.Maths
{
    public static class Numbers
    {
        public static double Clamp(double x, double min, double max)
        {
            return (x < min) ? min : (x > max ? max : x);
        }

        public static double Saturate(double x)
        {
            return Clamp(x, 0, 1);
        }
    }
}
