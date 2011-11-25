using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sora.GameEngine.GameComponents.Scene;
using Microsoft.Xna.Framework.Graphics;
using Sora.GameEngine.Offscreen;
using Sora.GameEngine.GameComponents.Cameras;
using Microsoft.Xna.Framework;

namespace KinectBrowser.D3D
{
    public class SoraEngineScreen: OffscreenEngineVirtualScreen
    {
        Node screenContent;

        public Node ScreenContent
        {
            get { return screenContent; }
        }

        public FixedCamera DefaultCamera { get; private set; }

        public SoraEngineHost Host { get; private set; }

        public Node CursorContent { get; private set; }

        public SoraEngineScreen(SoraEngineHost host)
            : base(host.CurrentEngine)
        {
        }

        public override void Draw(GameTime gameTime)
        {
            var device = CurrentEngine.Device;

            device.Clear(Color.Pink);

            base.Draw(gameTime);
        }

        protected override void LoadScreenContent()
        {
            base.LoadScreenContent();

            CameraManager.LoadAndSetActiveCamera(DefaultCamera = new FixedCamera(CurrentEngine) { NearPlane = 0.1f, FarPlane = 100f, Position = new Vector3(0, 0, -1), Target = new Vector3(0,0,0) });

            screenContent = new Node(LocalContent);

            CurrentEngine.SceneManager.Root.Add(screenContent);
        }
    }
}
