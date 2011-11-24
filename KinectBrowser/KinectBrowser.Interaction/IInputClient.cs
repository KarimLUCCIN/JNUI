using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectBrowser.Interaction.Maths;
using Microsoft.Xna.Framework;

namespace KinectBrowser.Interaction
{
    public interface IInputClient
    {
        Rectangle ScreenArea { get; }

        Rectangle ClientArea { get; }
    }
}
