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

            TabsNode.Add(new_page.D3DNode);
            ReOrderTabsNode();

            activePage = new_page;

            RaiseTabCountChanged();

            return new_page;
        }

        private void ReOrderTabsNode()
        {
            int count = TabCount;
            if (count > 0)
            {
                var step = 2 * Math.PI / (double)count;

                double radius = count * 0.8f;
                double angle = 0;

                for (int i = 0; i < count; i++)
                {
                    var node = TabsNode[i];
                    node.Position = new Microsoft.Xna.Framework.Vector3((float)(Math.Cos(angle) * radius), 0, (float)(Math.Sin(angle) * radius));

                    angle += step;
                }

                D3DScreen.CurrentEngine.CameraManager.ActiveCamera.Position = new Microsoft.Xna.Framework.Vector3(0, 0, (float)(-radius - 1.8f));
            }
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
                activePage.Handle_MouseMove(e.GetPosition(this));
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
