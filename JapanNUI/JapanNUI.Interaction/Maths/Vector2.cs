using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction.Maths
{
    public struct Vector2
    {
        public double X;
        public double Y;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator+(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2 operator *(double a, Vector2 b)
        {
            return new Vector2(a * b.X, a * b.Y);
        }

        public static Vector2 operator *(Vector2 a, double b)
        {
            return b * a;
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        public double LengthSquared()
        {
            return X * X + Y * Y;
        }

        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        public static double Distance(ref Vector2 a, ref Vector2 b)
        {
            return (b - a).Length();
        }

        public static double Dot(ref Vector2 a, ref Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public Vector2 Normalize()
        {
            var l = Length();

            if (l <= 0)
                return new Vector2(double.NaN, double.NaN);
            else
                return (1 / l) * this;
        }

        static Vector2 zero = new Vector2(0, 0);

        public static Vector2 Zero
        {
            get { return zero; }
        }

        public override string ToString()
        {
            return String.Format("({0},{1})", X, Y);
        }

        public static Vector2 Clamp(Vector2 x, Vector2 min, Vector2 max)
        {
            return new Vector2(Numbers.Clamp(x.X, min.X, max.X), Numbers.Clamp(x.Y, min.Y, max.Y));
        }
    }
}
