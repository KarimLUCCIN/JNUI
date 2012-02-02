//#define DEBUG_VIEW

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
using System.Windows.Interop;
using Sora.GameEngine.GameComponents.Scene;
using Sora.GameEngine.GameComponents.Animations;
using Microsoft.Xna.Framework;
using Sora.GameEngine.GameComponents.Cameras;
using Awesomium.Core;
using System.ComponentModel;
using KinectBrowser.D3D.Shaders;
using Sora.GameEngine.GameComponents.SceneObjects;

namespace KinectBrowser.D3D.Browser
{
    /// <summary>
    /// Interaction logic for D3DBrowser.xaml
    /// </summary>
    public partial class D3DBrowser : UserControl, INotifyPropertyChanged
    {
#if(DEBUG_VIEW)
        private bool debugView = true;
#else
        private bool debugView = false;
#endif

        public bool DebugView
        {
            get { return debugView; }
            set { debugView = value; }
        }


        bool isActive = true;

        /// <summary>
        /// Obtient ou définit une valeur indiquant si le browser contrôle actuellement l'input à savoir souris et clavier
        /// </summary>
        public bool IsActive
        {
            get { return isActive && d3DBrowserFocusTrap.IsFocused; }
            set { isActive = value; }
        }

        public SoraEngineHost Host { get; private set; }


        public SoraEngineScreen D3DScreen
        {
            get { return Host.RenderingScreen; }
        }

        public SceneObjectTexturedQuad DebugTextureQuad { get; private set; }

        public D3DBrowser()
        {
            IsActive = true;

            var wConfig = new WebCoreConfig();
            wConfig.LogLevel = LogLevel.Verbose;
            wConfig.LogPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\web.log";
            wConfig.EnableVisualStyles = false;
            WebCore.Initialize(wConfig);

            InitializeComponent();
        }

        /// <summary>
        /// Attache l'instance du Browser avec un moteur de rendu
        /// </summary>
        /// <param name="host"></param>
        public void Attach(SoraEngineHost host)
        {
            if (host == null)
                throw new ArgumentNullException("host");

            Host = host;

            d3dImageContent.Source = Host.InteropImage;

            var hwndSrc = HwndSource.FromDependencyObject(this) as HwndSource;
            hwndSrc.AddHook(WndProc);

            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);


            TabsNode = new Node(D3DScreen.LocalContent);

            D3DScreen.ScreenContent.Add(TabsNode);

            DebugTextureQuad = new SceneObjectTexturedQuad(D3DScreen.LocalContent);
            DebugTextureQuad.CompositionTex = host.CurrentEngine.Renderer.CompositionTexManager.TexColorOnly;
            DebugTextureQuad.Rotation = new Sora.GameEngine.MathUtils.RotationVector(MathHelper.Pi, 0, 0);

            D3DScreen.ScreenContent.Add(DebugTextureQuad);

            var bEffect = new BackgroundFill_Effect(D3DScreen.LocalContent);
            bEffect.LoadContent();

            D3DScreen.CurrentEngine.Renderer.CustomClearEffect = bEffect;

            //Composer = new Composer_Effect(D3DScreen.LocalContent);
            //Composer.LoadContent();

            //D3DScreen.ScreenContent.Add(Composer);
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            WebCore.Update();

            RenderUpdate();

            Host.Render();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (IsActive && activePage != null)
                return activePage.Handle_WndProc(hwnd, msg, wParam, lParam, ref handled);
            else
                return IntPtr.Zero;
        }

        private List<D3DBrowserTab> internalTabs = new List<D3DBrowserTab>();

        public Node TabsNode { get; private set; }

        public D3DBrowserTab[] Tabs
        {
            get { return internalTabs.ToArray(); }
        }

        public int TabCount
        {
            get { return internalTabs.Count; }
        }

        D3DBrowserTab activePage = null;

        public D3DBrowserTab ActivePage
        {
            get { return activePage; }
            set
            {
                if (activePage != null && activePage != value && value != null && internalTabs.Contains(value))
                {
                    activePage.Active = false;
                    value.Active = true;

                    activePage = value;

                    RaiseActivePageChanged();
                }
            }
        }

        public D3DBrowserTab NewTab(string url)
        {
            var new_page = new D3DBrowserTab(this, Host.RenderingWidth, Host.RenderingHeight);
            new_page.Navigate(url);

            internalTabs.Add(new_page);
            ActivePage = new_page;
            activePage = new_page;

            TabsNode.Add(new_page.D3DNode);

            ReOrderTabsNode();

            RaiseTabCountChanged();

            return new_page;
        }

        public void TabNext()
        {
            var index = internalTabs.IndexOf(activePage);

            if (index >= 0)
            {
                if (index == internalTabs.Count - 1)
                {
                    CurrentCamTabAngle = (float)(CurrentCamTabAngle - Math.PI * 2);
                }

                ActivePage = internalTabs[(index + 1) % internalTabs.Count];
                MoveCameraTo(CurrentCamRadius, (float)ActivePage.CurrentAngle);
            }
        }

        public void TabPrev()
        {
            var index = internalTabs.IndexOf(activePage);

            if (index >= 0)
            {
                if (index == 0)
                {
                    CurrentCamTabAngle = (float)(Math.PI * 2 - CurrentCamTabAngle);
                }

                ActivePage = internalTabs[(((index - 1) % internalTabs.Count) + internalTabs.Count) % internalTabs.Count];
                MoveCameraTo(CurrentCamRadius, (float)ActivePage.CurrentAngle);
            }
        }

        private float currentCamRadius = 0;

        public float CurrentCamRadius
        {
            get { return currentCamRadius; }
            set { currentCamRadius = value; }
        }

        private float currentCamTabAngle = 0;

        public float CurrentCamTabAngle
        {
            get { return currentCamTabAngle; }
            set { currentCamTabAngle = value; }
        }

        private float radiusOffsetNormal = 1.85f;

        public float RadiusOffsetNormal
        {
            get { return radiusOffsetNormal; }
            set { radiusOffsetNormal = value; }
        }

        private float radiusOffsetMovement = 2.6f;

        public float RadiusOffsetMovement
        {
            get { return radiusOffsetMovement; }
            set { radiusOffsetMovement = value; }
        }

        private void ReOrderTabsNode()
        {
            int count = TabCount;
            if (count > 0)
            {
                var step = 2 * Math.PI / (double)count;

                double radius = count * 0.8f;
                double angle = 0;

                var activeTabAngle = 0.0;

                for (int i = 0; i < count; i++)
                {
                    internalTabs[i].CurrentAngle = angle;

                    var node = TabsNode[i];

                    node.Position = new Microsoft.Xna.Framework.Vector3((float)(Math.Cos(angle) * radius), 0, (float)(Math.Sin(angle) * radius));
                    node.Rotation = new Sora.GameEngine.MathUtils.RotationVector((float)(-angle - MathHelper.PiOver2), 0, 0);

                    if (activePage == internalTabs[i])
                        activeTabAngle = angle;

                    angle += step;
                }

                MoveCameraTo((float)radius, (float)activeTabAngle);
            }
        }

        private void MoveCameraTo(float activeRadius, float activeTabAngle)
        {
            if (debugView)
            {
                activeTabAngle = MathHelper.PiOver2;
            }

            var cam = D3DScreen.CurrentEngine.CameraManager.ActiveCamera;

            var v2Anim = new AnimationVector3(D3DScreen.CurrentEngine,
                TimeSpan.FromSeconds(0.8),
                new KeyValuePair<float, Vector3>(0, new Vector3() { X = CurrentCamRadius, Y = radiusOffsetNormal, Z = CurrentCamTabAngle }),
                new KeyValuePair<float, Vector3>(0.4f, new Vector3() { X = CurrentCamRadius, Y = radiusOffsetMovement, Z = CurrentCamTabAngle * 0.6f + activeTabAngle * 0.4f }),
                new KeyValuePair<float, Vector3>(0.6f, new Vector3() { X = activeRadius, Y = radiusOffsetMovement, Z = CurrentCamTabAngle * 0.4f + activeTabAngle * 0.6f }),
                new KeyValuePair<float, Vector3>(1, new Vector3() { X = activeRadius, Y = radiusOffsetNormal, Z = activeTabAngle }));

            ExecuteCameraAnimation(cam, v2Anim);

            D3DScreen.CurrentEngine.AnimationManager.Start(v2Anim);
        }

        private void ExecuteCameraAnimation(BaseCamera cam, AnimationVector3 v2Anim)
        {
            v2Anim.Animated += delegate
            {
                var angle = v2Anim.Current.Z;
                var radius = v2Anim.Current.X;

                CurrentCamRadius = radius;
                CurrentCamTabAngle = angle;

                radius += v2Anim.Current.Y;

                cam.Position = new Microsoft.Xna.Framework.Vector3((float)(Math.Cos(angle) * radius), 0, (float)(Math.Sin(angle) * radius));
            };
        }

        public void Close(D3DBrowserTab page)
        {
            if (page != null && internalTabs.Contains(page))
            {
                int activeIndex = activePage == page ? internalTabs.IndexOf(activePage) : -1;

                internalTabs.Remove(page);

                TabsNode.Remove(page.D3DNode);
                ReOrderTabsNode();

                page.CloseInternal();

                if (activeIndex >= 0)
                {
                    activePage = null;

                    if (activeIndex >= internalTabs.Count)
                        activeIndex = internalTabs.Count - 1;

                    if (activeIndex >= 0)
                    {
                        activePage = internalTabs[activeIndex];
                        activePage.Active = true;
                    }
                }

                ReOrderTabsNode();
                RaiseTabCountChanged();
            }
        }

        private void RenderUpdate()
        {
            DebugTextureQuad.Visible = debugView;

            for (int i = 0; i < internalTabs.Count; i++)
            {
                if (debugView)
                    internalTabs[i].D3DNode.Visible = false;
             
                internalTabs[i].RenderUpdate();
            }
        }

        #region Mouse

        private bool customInput = false;

        /// <summary>
        /// Indique si l'input est générée à partir de Windows ou si elle doit être explicitement faite
        /// via CustomInput_*
        /// </summary>
        public bool CustomInput
        {
            get { return customInput; }
            set { customInput = value; }
        }

        public void CustomInput_MouseDown(MouseButtonEventArgs e)
        {
            if (isActive && !d3DBrowserFocusTrap.IsFocused)
                d3DBrowserFocusTrap.Focus();

            if (activePage != null)
                activePage.Handle_MouseDown(this, e);
        }

        public void CustomInput_MouseMove(System.Windows.Point position)
        {
            if (activePage != null)
                activePage.Handle_MouseMove(OffsetMousePosition(position));
        }

        public void CustomInput_MouseUp(MouseButtonEventArgs e)
        {
            if (activePage != null)
                activePage.Handle_MouseUp(this, e);
        }

        public void CustomInput_MouseWheel(int delta)
        {
            if (activePage != null)
                activePage.Handle_MouseWheel(delta);
        }


        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!customInput)
            {
                if (isActive && !d3DBrowserFocusTrap.IsFocused)
                    d3DBrowserFocusTrap.Focus();

                if (activePage != null)
                    activePage.Handle_MouseDown(sender, e);
            }
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (!customInput)
            {
                if (activePage != null)
                    activePage.Handle_MouseMove(OffsetMousePosition(e.GetPosition(this)));
            }
        }

        /* Il y a une marge sur les bords */
        internal int InternalBrowserMargin
        {
            get { return 26; }
        }

        private System.Windows.Point OffsetMousePosition(System.Windows.Point point)
        {
            var marginX = InternalBrowserMargin; // +activePage.InternalMarginX;
            var marginY = InternalBrowserMargin + 6; // +activePage.InternalMarginY;

            var ratioX = (double)ActualWidth / (double)(ActualWidth - InternalBrowserMargin * 2 - activePage.InternalMarginX * 2);
            var ratioY = (double)ActualHeight / (double)(ActualHeight - InternalBrowserMargin * 2 - activePage.InternalMarginY * 2);

            point.X = ratioX * Math.Max(0, Math.Min(point.X - marginX, ActualWidth - marginX * 2));
            point.Y = ratioY * Math.Max(0, Math.Min(point.Y - marginY, ActualHeight - marginY * 2));

            return point;
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!customInput)
            {
                if (activePage != null)
                    activePage.Handle_MouseUp(sender, e);
            }
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!customInput)
            {
                if (activePage != null)
                    activePage.Handle_MouseWheel(e.Delta);
            }
        }

        #endregion

        #region Events

        public event EventHandler TabCountChanged;

        private void RefreshCapabilities()
        {
            RaisePropertyChanged("CanGoBack");
            RaisePropertyChanged("CanGoForward");
        }

        private void RaiseTabCountChanged()
        {
            Dispatcher.Invoke((Action)delegate
                  {
                      if (TabCountChanged != null)
                      {
                          TabCountChanged(this, EventArgs.Empty);
                      }

                      RefreshCapabilities();
                  });
        }

        public event EventHandler ActivePageChanged;

        private void RaiseActivePageChanged()
        {
            Dispatcher.Invoke((Action)delegate
            {
                if (ActivePageChanged != null)
                {
                    ActivePageChanged(this, EventArgs.Empty);
                }

                RefreshCapabilities();
            });
        }

        public event EventHandler ActivePageTitleChanged;

        internal void RaiseActivePageTitleChanged()
        {
            if (ActivePageTitleChanged != null)
            {
                Dispatcher.Invoke((Action)delegate
                {
                    ActivePageTitleChanged(this, EventArgs.Empty);
                });
            }
        }

        public event EventHandler ActivePageTitleBeginNavigation;

        internal void RaiseActivePageTitleBeginNavigation()
        {
            if (ActivePageTitleBeginNavigation != null)
            {
                Dispatcher.Invoke((Action)delegate
                {
                    ActivePageTitleBeginNavigation(this, EventArgs.Empty);
                });
            }
        }

        public event EventHandler ActivePageLoadCompleted;

        internal void RaiseActivePageLoadCompleted()
        {
            Dispatcher.Invoke((Action)delegate
            {
                if (ActivePageLoadCompleted != null)
                {
                    ActivePageLoadCompleted(this, EventArgs.Empty);
                }

                RefreshCapabilities();
            });
        }
        #endregion

        public bool CanGoBack
        {
            get
            {
                return activePage != null && activePage.CanGoBack;
            }
        }

        public bool CanGoForward
        {
            get
            {
                return activePage != null && activePage.CanGoForward;
            }
        }

        public void GoBack()
        {
            if (activePage != null)
            {
                activePage.GoBack();

                RefreshCapabilities();
            }
        }

        public void Reload()
        {
            if (activePage != null)
            {
                activePage.Reload();

                RefreshCapabilities();
            }
        }

        public void GoForward()
        {
            if (activePage != null)
            {
                activePage.GoForward();

                RefreshCapabilities();
            }
        }

        public void Scroll(int amountY, int amountX)
        {
            if (activePage != null)
            {
                activePage.Scroll(amountX, amountY);
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        #endregion
    }
}
