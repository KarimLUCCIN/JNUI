using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JapanNUI.Interaction.Maths;

namespace JapanNUI.Interaction
{
    public interface IInputListener
    {
        Rectangle ScreenArea { get; }

        Rectangle ClientArea { get; }

        void Update(IInputProvider provider);

        void DebugDisplayBgr32DepthImage(int width, int height, byte[] convertedDepthFrame, int stide);

        void UpdatePrimaryCursor(Vector3 position);
        void UpdateSecondaryCursor(Vector3 position);

        void ContextDelegateMethod(Action action);
    }
}
