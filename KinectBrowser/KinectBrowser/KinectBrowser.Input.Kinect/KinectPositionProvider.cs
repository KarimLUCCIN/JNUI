using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction;
using KinectBrowser.Interaction.Maths;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Input.Kinect
{
    public class KinectPositionProvider : BasePositionProvider
    {
        public KinectProvider KinectProvider { get; private set; }

        public KinectPositionProvider(string id, KinectProvider kinectProvider)
            : base(id)
        {
            KinectProvider = kinectProvider;

            CurrentPoint.HistorySize = 10;
            CurrentPoint.PixelMoveTreshold = 10;
            CurrentPoint.UpdateLatency = 0.25f;
        }

        private static Vector2 XY(ref Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static Vector2 RelativePointToAbsolutePoint(Vector2 point, Rectangle rectangle)
        {
            var size = new Vector2(rectangle.Width, rectangle.Height);
            var origin = new Vector2(rectangle.X, rectangle.Y);

            return new Vector2((point.X * size.X) + origin.X, (point.Y * size.Y) + origin.Y);
        }

        public bool Update(Vector3 skeletonPosition, CursorState state)
        {
            if (BeginUpdate())
            {
                try
                {
                    var input = KinectProvider.Client;

                    var localPoint = XY(ref skeletonPosition);

                    var clientMousePos = RelativePointToAbsolutePoint(localPoint, input.ClientArea);

                    var origin = new Vector2(input.ClientArea.X, input.ClientArea.Y);
                    CurrentPoint.UpdatePosition(new Vector3(clientMousePos - origin, skeletonPosition.Z), state);
                }
                finally
                {
                    EndUpdate();
                }

                return true;
            }
            else
                return false;
        }
    }
}
