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
using Awesomium.Core;
using Sora.GameEngine.GameComponents.Scene;
using Sora.GameEngine.GameComponents.Animations;
using Microsoft.Xna.Framework;
using Sora.GameEngine.GameComponents.Cameras;

namespace wpf_3d
{
    /// <summary>
    /// Interaction logic for WebBrowser3D.xaml
    /// </summary>
    public partial class WebBrowser3D : UserControl
    {
        public D3DEffectsScreen D3DScreen { get; private set; }

        private List<WebView3DContainer> internalTabs = new List<WebView3DContainer>();

        public Node TabsNode { get; private set; }

        public WebView3DContainer[] Tabs
        {
            get { return internalTabs.ToArray(); }
        }

        public int TabCount
        {
            get { return internalTabs.Count; }
        }

        WebView3DContainer activePage = null;

        public WebView3DContainer ActivePage
        {
            get { return activePage; }
            set
            {
                if (activePage != null && activePage != value && value != null && internalTabs.Contains(value))
                {
                    activePage.Active = false;
                    value.Active = true;

                    activePage = value;
                }
            }
        }

        public event EventHandler TabCountChanged;

        private void RaiseTabCountChanged()
        {
            if (TabCountChanged != null)
                TabCountChanged(this, EventArgs.Empty);
        }

        public WebBrowser3D()
        {
            InitializeComponent();
        }

        public void InitializeRenderScreen(D3DEffectsScreen d3dScreen)
        {
            if (d3dScreen == null)
                throw new ArgumentNullException("d3dScreen");

            D3DScreen = d3dScreen;
            TabsNode = new Node(D3DScreen.LocalContent);

            d3dScreen.ScreenContent.Add(TabsNode);
            d3dImageContent.Source = d3dScreen.D3DImageSource;
        }

        public WebView3DContainer NewTab(string url)
        {
            var new_page = new WebView3DContainer(this, D3DScreen.CompositionWidth, D3DScreen.CompositionHeight);
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

                for (int i = 0; i < count ; i++)
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

        public void Close(WebView3DContainer page)
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

                RaiseTabCountChanged();
            }
        }

        public void RenderUpdate()
        {
            for (int i = 0; i < internalTabs.Count; i++)
            {
                internalTabs[i].RenderUpdate();
            }
        }

        public IntPtr Handle_WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (activePage != null)
                return activePage.Handle_WndProc(hwnd, msg, wParam, lParam, ref handled);
            else
                return IntPtr.Zero;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (activePage != null)
                activePage.Handle_MouseDown(sender, e);
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (activePage != null)
                activePage.Handle_MouseMove(OffsetMousePosition(e.GetPosition(this)));
        }

        private System.Windows.Point OffsetMousePosition(System.Windows.Point point)
        {
            /* Il y a une marge sur les bords */
            var margin = 26;

            var ratioX = (double)ActualWidth / (double)(ActualWidth - margin * 2);
            var ratioY = (double)ActualHeight / (double)(ActualHeight - margin * 2);

            point.X = ratioX * Math.Max(0, Math.Min(point.X - margin, ActualWidth - margin * 2));
            point.Y = ratioY * Math.Max(0, Math.Min(point.Y - margin, ActualHeight - margin * 2));

            return point;
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (activePage != null)
                activePage.Handle_MouseUp(sender, e);
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (activePage != null)
                activePage.Handle_MouseWheel(e.Delta);
        }
    }
}
