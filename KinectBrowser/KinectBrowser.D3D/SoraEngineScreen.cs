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

        FixedCamera cam;

        protected override void LoadScreenContent()
        {
            base.LoadScreenContent();

            cam = new FixedCamera(CurrentEngine) { NearPlane = 0.1f, FarPlane = 100f, Position = new Vector3(0, 0, -1), Target = new Vector3(0,0,0) };
            //cam = new FixedOrthographicCamera(CurrentEngine) { ProjectionHeight = 3 * 0.785f, ProjectionWidth = 2 * 1.33f * 0.8f, NearPlane = 0.1f, FarPlane = 100f, Position = new Vector3(0, 0, -1), Target = new Vector3(0, 0, 0) };

            CameraManager.LoadAndSetActiveCamera(DefaultCamera = cam);

            screenContent = new Node(LocalContent);

            CurrentEngine.SceneManager.Root.Add(screenContent);
        }

        //public override void Update(GameTime gameTime, bool canGetFocusInput, bool hasMainScreen)
        //{
        //    base.Update(gameTime, canGetFocusInput, hasMainScreen);

        //    CurrentEngine.InputManager.Update(gameTime);
        //    if (CurrentEngine.InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Up))
        //    {
        //        cam.ProjectionHeight += 0.001f;
        //        cam.UpdateProjection();
        //    }
        //    if (CurrentEngine.InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Down))
        //    {
        //        cam.ProjectionHeight -= 0.001f;
        //        cam.UpdateProjection();
        //    }
        //    if (CurrentEngine.InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Left))
        //    {
        //        cam.ProjectionWidth -= 0.001f;
        //        cam.UpdateProjection();
        //    }
        //    if (CurrentEngine.InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Right))
        //    {
        //        cam.ProjectionWidth += 0.001f;
        //        cam.UpdateProjection();
        //    }
        //}
    }
}
