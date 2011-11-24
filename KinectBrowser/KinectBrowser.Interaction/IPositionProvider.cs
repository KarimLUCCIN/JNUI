using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Maths;
using KinectBrowser.Interaction.Gestures;

namespace KinectBrowser.Interaction
{
    public interface IPositionProvider
    {
        GesturePoint CurrentPoint { get; }

        string Id { get; }
    }
}
