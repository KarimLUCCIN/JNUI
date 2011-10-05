using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapanNUI.Interaction.Maths
{
    public struct Rectangle
    {
        public Vector2 Origin;
        public Vector2 Size;

        public Rectangle(double x, double y, double width, double height)
            :this(new Vector2(x,y), new Vector2(width, height))
        {

        }

        public Rectangle(Vector2 origin, Vector2 size)
        {
            Origin = origin;
            Size = size;
        }

        public Vector2 AbsolutePointToRelativePoint(Vector2 point)
        {
            return new Vector2((point.X - Origin.X) / Size.X, (point.Y - Origin.Y) / Size.Y);
        }

        public Vector2 RelativePointToAbsolutePoint(Vector2 point)
        {
            return new Vector2((point.X * Size.X) + Origin.X, (point.Y * Size.Y) + Origin.Y);
        }

        public override string ToString()
        {
            return String.Format("[{0}, {1}]", Origin, Size);
        }
    }
}
