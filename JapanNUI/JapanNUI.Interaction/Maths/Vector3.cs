using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction.Maths
{
    public struct Vector3
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3(Vector2 xy, double z)
            :this(xy.X, xy.Y, z)
        {

        }

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 operator+(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator *(double a, Vector3 b)
        {
            return new Vector3(a * b.X, a * b.Y, a * b.Z);
        }

        public static Vector3 operator *(Vector3 a, double b)
        {
            return b * a;
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public double LengthSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static double Distance(ref Vector3 a, ref Vector3 b)
        {
            return (b - a).Length();
        }

        public static double Dot(ref Vector3 a, ref Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public Vector3 Normalize()
        {
            var l = Length();

            if (l <= 0)
                return new Vector3(double.NaN, double.NaN, double.NaN);
            else
                return (1 / l) * this;
        }

        static Vector3 zero = new Vector3(0, 0, 0);

        public static Vector3 Zero
        {
            get { return zero; }
        }

        public override string ToString()
        {
            return String.Format("({0},{1},{2})", X, Y, Z);
        }

        public static Vector3 Clamp(Vector3 x, Vector3 min, Vector3 max)
        {
            return new Vector3(Numbers.Clamp(x.X, min.X, max.X), Numbers.Clamp(x.Y, min.Y, max.Y), Numbers.Clamp(x.Z, min.Z, max.Z));
        }
    }
}
