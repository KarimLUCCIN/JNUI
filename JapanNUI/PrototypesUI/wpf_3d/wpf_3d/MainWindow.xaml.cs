using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Sora.GameEngine.Offscreen;
using Awesomium.Core;
using System.Windows.Interop;
using Microsoft.Xna.Framework.Graphics;
using Sora.GameEngine.GameComponents.GameSystem.Rendering;

namespace wpf_3d
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public OffscreenEngine soraEngine;
        public D3DEffectsScreen d3dEffectsScreen;

        public WebView webView;

        public MainWindow()
        {
            InitializeComponent();

            Closed += new EventHandler(MainWindow_Closed);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            // Shut down Awesomium before exiting.
            WebCore.Shutdown(); 
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var d3dImage = new D3DImage(96, 96);
            OffscreenEngineBuilder.MultiSampleCount = 4;
            RenderTargetManager.DisableMultiSampling = false;

            soraEngine = OffscreenEngineBuilder.CreateFromD3DImage(800, 600, d3dImage);

            OffscreenEngineBuilder.MultiSampleCount = 0;
            RenderTargetManager.DisableMultiSampling = true;

            soraEngine.Renderer.EnableDebugKeyboardCommands = false;

            soraEngine.Renderer.EnableGlow = false;
            soraEngine.Renderer.EnableHDR = false;
            soraEngine.Renderer.EnableLighting = false;
            soraEngine.Renderer.EnableMotionBlur = false;
            soraEngine.Renderer.EnableShadow = false;
            soraEngine.Renderer.EnableToonShading = false;

            soraEngine.ScreenManager.AddScreen(d3dEffectsScreen = new D3DEffectsScreen(soraEngine));

            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);

            webBrowserInst.InitializeRenderScreen(d3dEffectsScreen);

            webBrowserInst.NewTab("http://www.google.com");
            webBrowserInst.NewTab("http://www.youtube.com");
            webBrowserInst.NewTab("http://www.wikipedia.com");
            webBrowserInst.NewTab("http://www.youtube.com/watch?v=jn4VUKPObvI&feature=bf_next&list=PL5C078C39C0029210&lf=mh_lolz");
        }

        TimeSpan lastRendering = TimeSpan.Zero;
        TimeSpan drawAccumulator = TimeSpan.FromSeconds(1);

        object dxLock = new object();

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;

            var elapsed = args.RenderingTime - lastRendering;
            lastRendering = args.RenderingTime;

            drawAccumulator += elapsed;

            WebCore.Update();

            /* debug */
            var inputManger = soraEngine.InputManager;
            inputManger.UpdateKeyboardState();

            var unitDisp = 0.01f;

            if (inputManger.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Down))
            {
                soraEngine.CameraManager.ActiveCamera.Position += Microsoft.Xna.Framework.Vector3.Forward * unitDisp;
            } 
            else if (inputManger.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Up))
            {
                soraEngine.CameraManager.ActiveCamera.Position += Microsoft.Xna.Framework.Vector3.Backward * unitDisp;
            }
            else if (inputManger.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Left))
            {
                soraEngine.CameraManager.ActiveCamera.Position += Microsoft.Xna.Framework.Vector3.Left * unitDisp;
            }
            else if (inputManger.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Right))
            {
                soraEngine.CameraManager.ActiveCamera.Position += Microsoft.Xna.Framework.Vector3.Right * unitDisp;
            }

            if (drawAccumulator.TotalSeconds > 1 / 40.0)
            {
                webBrowserInst.RenderUpdate();

                lock (dxLock)
                {
                    soraEngine.EngineUpdate(drawAccumulator);

                    drawAccumulator = TimeSpan.Zero;

                    soraEngine.Renderer.RendererMode = RendererMode.ColorOnly;
                    soraEngine.RenderToImage();
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {            
            base.OnSourceInitialized(e);

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;

            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return webBrowserInst.Handle_WndProc(hwnd, msg, wParam, lParam, ref handled);
        }
    }
}
