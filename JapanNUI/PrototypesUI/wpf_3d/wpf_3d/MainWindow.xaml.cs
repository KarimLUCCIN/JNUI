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

namespace wpf_3d
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public OffscreenEngineWpf soraEngine;
        public D3DEffectsScreen d3dEffectsScreen;

        bool is3DRendering = false;

        public MainWindow()
        {
            InitializeComponent();

            lolBrowser.Navigate("http://www.google.com");
            d3dContent.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            soraEngine = OffscreenEngineBuilder.Create((int)d3dContent.Width, (int)d3dContent.Height, d3dImage);

            soraEngine.Renderer.EnableGlow = false;
            soraEngine.Renderer.EnableHDR = false;
            soraEngine.Renderer.EnableLighting = false;
            soraEngine.Renderer.EnableMotionBlur = false;
            soraEngine.Renderer.EnableShadow = false;
            soraEngine.Renderer.EnableToonShading = false;

            soraEngine.ScreenManager.AddScreen(d3dEffectsScreen = new D3DEffectsScreen(soraEngine));

            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
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

            if (is3DRendering)
            {
                if (drawAccumulator.TotalSeconds > 1 / 40.0 && d3dImage.IsFrontBufferAvailable)
                {
                    if (recentlySwitched)
                    {
                        recentlySwitched = false;
                        drawAccumulator = TimeSpan.FromSeconds(1 / 60.0f);
                    }

                    lock (dxLock)
                    {
                        soraEngine.EngineUpdate(drawAccumulator);

                        drawAccumulator = TimeSpan.Zero;

                        soraEngine.RenderToImage();
                    }
                }
            }
            else
            {
                drawAccumulator = TimeSpan.Zero;
            }
        }

        private void switchBtn_Click(object sender, RoutedEventArgs e)
        {
            Switch();
        }

        bool recentlySwitched = false;

        private void Switch()
        {
            if (mainContent.Visibility == System.Windows.Visibility.Visible)
            {
                lock (dxLock)
                {
                    d3dEffectsScreen.DrawToTexture(lolBrowser);
                }

                d3dEffectsScreen.StartAnim();

                mainContent.Visibility = System.Windows.Visibility.Hidden;
                d3dContent.Visibility = System.Windows.Visibility.Visible;

                is3DRendering = true;
                recentlySwitched = true;
                drawAccumulator = TimeSpan.FromSeconds(1);
            }
            else
            {
                mainContent.Visibility = System.Windows.Visibility.Visible;
                d3dContent.Visibility = System.Windows.Visibility.Hidden;

                is3DRendering = false;
            }
        }
    }
}
