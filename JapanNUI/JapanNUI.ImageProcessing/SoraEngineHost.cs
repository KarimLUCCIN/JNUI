using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sora.GameEngine;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;

namespace JapanNUI.ImageProcessing
{
    public class SoraEngineHost : EngineDesignManager
    {
        static Form hiddenHost;

        public SoraEngineHost()
            : base(GenerateSettingsAndHost())
        {
            CurrentGame = this;

            CurrentGame.GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;

            InitializeForNullOutput();
            EngineIteration();
        }

        protected override bool EnablePhysicsEngine
        {
            get
            {
                return false;
            }
        }

        protected override bool EnableDefaultMenuFonts
        {
            get
            {
                return false;
            }
        }

        private static GameSettings GenerateSettingsAndHost()
        {
            if (hiddenHost == null)
            {
                hiddenHost = new Form();

                hiddenHost.Width = 4;
                hiddenHost.Height = 4;

                hiddenHost.Visible = false;

                hiddenHost.CreateControl();

                DeviceHandle = hiddenHost.Handle;
            }

            var result = new GameSettings();

            result.FullScreen = false;
            result.ResolutionWidth = 4;
            result.ResolutionHeight = 4;

            return result;
        }

        bool disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                hiddenHost.Dispose();
                hiddenHost = null;

                base.Dispose(disposing);
            }            
        }
    }
}
