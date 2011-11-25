using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sora.GameEngine.GameComponents.Scene;
using Microsoft.Xna.Framework.Graphics;
using Sora.GameEngine.Offscreen;
using Sora.GameEngine.GameComponents.Cameras;
using Microsoft.Xna.Framework;
using Sora.GameEngine.GameComponents.SceneObjects;

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

        /// <summary>
        /// Position du curseur, entre -1 et 1 sur les deux coordonnées
        /// </summary>
        public Vector2 CursorPosition { get; set; }

        private float cursorDepth = 0.2f;

        public float CursorDepth
        {
            get { return cursorDepth; }
            set { cursorDepth = value; }
        }
        
        public SoraEngineScreen(SoraEngineHost host)
            : base(host.CurrentEngine)
        {
            Host = host;
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
