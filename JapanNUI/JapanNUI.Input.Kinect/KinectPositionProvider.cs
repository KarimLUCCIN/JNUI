using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Input.Kinect
{
    public class KinectPositionProvider : BasePositionProvider
    {
        public KinectProvider KinectProvider { get; private set; }

        public KinectPositionProvider(string id, KinectProvider kinectProvider)
            :base(id)
        {
            KinectProvider = kinectProvider;
            
            CurrentPoint.HistorySize = 10;
            CurrentPoint.PixelMoveTreshold = 10;
            CurrentPoint.UpdateLatency = 0.25;
        }

        public bool Update(Vector3 skeletonPosition, CursorState state)
        {
            if (BeginUpdate())
            {
                try
                {
                    var input = KinectProvider.Listener;

                    var clientMousePos = input.ClientArea.RelativePointToAbsolutePoint(skeletonPosition.XY);

                    CurrentPoint.UpdatePosition(new Vector3(clientMousePos - input.ClientArea.Origin, skeletonPosition.Z), state);
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
