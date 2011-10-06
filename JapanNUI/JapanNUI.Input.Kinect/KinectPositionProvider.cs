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
        }

        public bool Update(Vector3 skeletonPosition)
        {
            if (BeginUpdate())
            {
                try
                {
                    var input = KinectProvider.Listener;

                    var clientMousePos = input.ClientArea.RelativePointToAbsolutePoint(skeletonPosition.XY);

                    CurrentPoint.UpdatePosition(new Vector3(clientMousePos - input.ClientArea.Origin, skeletonPosition.Z));
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
