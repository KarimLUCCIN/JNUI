using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sora.GameEngine.Offscreen;
using System.Windows.Interop;
using Sora.GameEngine.GameComponents.GameSystem.Rendering;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Reflection;

namespace KinectBrowser.D3D
{
    public class SoraEngineHost
    {
        /// <summary>
        /// Moteur 3D actuel
        /// </summary>
        public OffscreenEngine CurrentEngine { get; private set; }

        /// <summary>
        /// Image utilisée comme passerelle entre DirectX et WPF
        /// </summary>
        public D3DImage InteropImage { get; private set; }

        public int RenderingWidth { get; private set; }
        public int RenderingHeight { get; private set; }

        public SoraEngineScreen RenderingScreen { get; private set; }

        public GraphicsDevice Device
        {
            get { return CurrentEngine.Device; }
        }

        /// <summary>
        /// Initialize une nouvelle instance du moteur de rendu avec les dimensions spécifiées
        /// </summary>
        /// <remarks>Les dimensions sont figées et ne peuvent être changées par la suite ...</remarks>
        /// <param name="renderingWidth"></param>
        /// <param name="renderingHeight"></param>
        public SoraEngineHost(int renderingWidth, int renderingHeight)
        {
            if (renderingWidth <= 4)
                throw new ArgumentOutOfRangeException("renderingWidth <= 4");
            if (renderingHeight <= 4)
                throw new ArgumentOutOfRangeException("renderingHeight <= 4");

            RenderingWidth = renderingWidth;
            RenderingHeight = renderingHeight;
        }

        /// <summary>
        /// Initialize le moteur 3D utilisé pour le rendu et le processing
        /// </summary>
        public void Initialize()
        {
            InteropImage = new D3DImage(96, 96);
            OffscreenEngineBuilder.MultiSampleCount = 4;

            RenderTargetManager.DisableMultiSampling = false;

            CurrentEngine = OffscreenEngineBuilder.CreateFromD3DImage(RenderingWidth, RenderingHeight, InteropImage);

            OffscreenEngineBuilder.MultiSampleCount = 0;
            RenderTargetManager.DisableMultiSampling = true;

            CurrentEngine.Renderer.EnableDebugKeyboardCommands = false;

            CurrentEngine.Renderer.EnableGlow = false;
            CurrentEngine.Renderer.EnableHDR = false;
            CurrentEngine.Renderer.EnableLighting = false;
            CurrentEngine.Renderer.EnableMotionBlur = false;
            CurrentEngine.Renderer.EnableShadow = false;
            CurrentEngine.Renderer.EnableToonShading = false;
            CurrentEngine.Renderer.EnableFXAA = false;

            CurrentEngine.ScreenManager.AddScreen(RenderingScreen = new SoraEngineScreen(this));

            CurrentEngine.RenderingOptions.TextureQuality = Sora.GameEngine.GameComponents.GameSystem.Rendering.Options.TextureQuality.High;
        }

        Stopwatch renderingWatch = new Stopwatch();
        TimeSpan renderingInterval = TimeSpan.FromSeconds(1 / 40.0);
        TimeSpan drawAccumulator = TimeSpan.Zero;

        TimeSpan renderingDuration = TimeSpan.Zero;
        Stopwatch renderingDurationWatch = new Stopwatch();

        public TimeSpan LastRenderingDuration
        {
            get { return renderingDuration; }
        }

        /// <summary>
        /// Effectue un rendu et met à jour InteropImage
        /// </summary>
        public void Render()
        {
            renderingDurationWatch.Restart();

            TimeSpan elapsed;

            if (!renderingWatch.IsRunning)
            {
                /* premier rendu */
                elapsed = TimeSpan.FromSeconds(renderingInterval.TotalSeconds * 2);
            }
            else
            {
                renderingWatch.Stop();
                elapsed = renderingWatch.Elapsed;
            }

            drawAccumulator += elapsed;

            if (drawAccumulator >= renderingInterval)
            {
                var soraEngine = CurrentEngine;

                soraEngine.InputManager.UpdateKeyboardState();

                soraEngine.EngineUpdate(drawAccumulator);

                soraEngine.Renderer.RendererMode = RendererMode.ColorOnly;
                soraEngine.RenderToImage(drawAccumulator);

                drawAccumulator = TimeSpan.Zero;

                //if (false)
                //{
                //    using (var stream = new FileStream(@"h:\lol2.png", FileMode.Create))
                //    {
                //        var rt = ((OffscreenEngineWpf)soraEngine).CompositionRT;
                //        rt.SaveAsPng(stream, rt.Width, rt.Height);
                //    }
                //}
            }

            renderingWatch.Restart();

            renderingDurationWatch.Stop();
            renderingDuration = TimeSpan.FromMilliseconds(renderingDuration.TotalMilliseconds * 0.7 + renderingDurationWatch.ElapsedMilliseconds * 0.3);
        }

        /// <summary>
        /// Formats supportés : .jpg, .gif, .png
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Texture2D LoadLocalImage(string name)
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fullPath = Path.Combine(basePath, "Images", name);

            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                return Texture2D.FromStream(CurrentEngine.Device, stream);
            }
        }
    }
}
