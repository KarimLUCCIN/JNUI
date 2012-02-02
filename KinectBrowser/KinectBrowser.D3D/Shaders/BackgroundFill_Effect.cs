using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sora.GameEngine.GameComponents.SceneObjects;
using Sora.GameEngine.GameComponents.Scene.Interfaces;
using Sora.GameEngine.GameComponents.GameSystem.Rendering;
using Sora.GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KinectBrowser.D3D.Shaders
{
    public class BackgroundFill_Effect : SceneObjectParametized, ILoadableSceneObject, IPostRenderEffect
    {
        public override bool SkipDrawSortInsertionTest
        {
            get
            {
                return true;
            }
        }

        public BackgroundFill_Shader BackgroundFill_Shader { get; private set; }

        public BackgroundFill_Effect(ResourcesContext resourcesContext)
            : base(resourcesContext, null)
        {

        }

        Vector2 halfPixel;

        protected override void LoadResources()
        {
            base.LoadResources();

            halfPixel = new Vector2(0.5f / CurrentEngine.Width, 0.5f / CurrentEngine.Height);

            BackgroundFill_Shader = new BackgroundFill_Shader(CurrentEngine.Device);
        }

        public int PassCount
        {
            get { return 1; }
        }

        public void ApplyEffect(RenderTarget2D rt, int currentPass)
        {
            var shader = BackgroundFill_Shader;

            shader.halfPixel = halfPixel;

            shader.CurrentTechnique.Passes[0].Apply();

            CurrentEngine.Renderer.QuadRenderer.RenderFullScreen();
        }
    }
}
