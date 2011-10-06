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

        public KinectPositionProvider(KinectProvider kinectProvider)
        {
            KinectProvider = kinectProvider;
        }

        public void Update(Vector3 skeletonPosition)
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
            }
        }
    }
}
