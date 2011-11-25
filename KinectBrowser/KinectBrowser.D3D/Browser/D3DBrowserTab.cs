using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awesomium.Core;
using Sora.GameEngine.GameComponents.SceneObjects;
using Microsoft.Xna.Framework.Graphics;
using Sora.GameEngine.GameComponents.Scene;
using Sora.GameEngine;
using KinectBrowser.D3D.DirectX;
using System.Windows.Input;
using System.Windows;
using KinectBrowser.D3D.Win32;
using System.IO;

namespace KinectBrowser.D3D.Browser
{
    public class D3DBrowserTab
    {
        private Texture2D associatedTexture = null;

        internal Texture2D AssociatedTexture
        {
            get
            {
                if (IsTextureDisposed)
                {
                    associatedTexture = new Texture2D(D3DEngine.Device, textureWidth, textureHeight);

                    if (quadFront != null)
                        quadFront.Texture = associatedTexture;

                    if (quadBack != null)
                        quadBack.Texture = associatedTexture;
                }

                return associatedTexture;
            }
        }

        public Node D3DNode { get; private set; }

        internal bool IsTextureDisposed
        {
            get
            {
                return associatedTexture == null || associatedTexture.IsDisposed;
            }
        }

        bool invalidated = false;

        internal bool IsTextureInvalidated
        {
            get
            {
                return invalidated || associatedTexture == null || associatedTexture.IsDisposed;
            }
        }

        WebView webView;

        SceneObjectTexturedQuad quadFront;
        SceneObjectTexturedQuad quadBack;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public SoraEngineScreen D3DScreen
        {
            get { return Container.Host.RenderingScreen; }
        }

        public Engine D3DEngine
        {
            get { return Container.Host.CurrentEngine; }
        }

        bool active = false;

        public bool Active
        {
            get { return active; }
            internal set
            {
                if (active != value)
                {
                    active = value;

                    if (value)
                        webView.Focus();
                    else
                        webView.Unfocus();
                }
            }
        }

        public D3DBrowser Container { get; private set; }

        int textureWidth, textureHeight;

        internal int InternalMarginX { get { return - Container.InternalBrowserMargin; } }

        internal int InternalMarginY { get { return - Container.InternalBrowserMargin; } }

        public D3DBrowserTab(D3DBrowser container, int width, int height)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            Container = container;

            Width = width;
            Height = height;

            var margin = InternalMarginX * 2;

            textureWidth = width + margin;
            textureHeight = height + margin;

            CreateWebView(textureWidth, textureHeight);

            var resourcesContext = D3DScreen.LocalContent;

            D3DNode = new Node(resourcesContext);

            quadFront = new SceneObjectTexturedQuad(resourcesContext);
            quadFront.Texture = AssociatedTexture;
            quadFront.CompositionTex = D3DEngine.Renderer.CompositionTexManager.TexColorOnly;

            quadBack = new SceneObjectTexturedQuad(resourcesContext);
            quadBack.Texture = AssociatedTexture;
            quadBack.CompositionTex = D3DEngine.Renderer.CompositionTexManager.TexColorOnly;
            quadBack.Rotation = new Sora.GameEngine.MathUtils.RotationVector((float)(Math.PI), 0, 0);

            D3DNode.Add(quadFront);
            D3DNode.Add(quadBack);
        }

        private void CreateWebView(int width, int height)
        {
            webView = WebCore.CreateWebView(width, height);
            
            webView.LoadCompleted += new EventHandler(webView_LoadCompleted);
            webView.OpenExternalLink += new OpenExternalLinkEventHandler(webView_OpenExternalLink);
            webView.BeginNavigation += new BeginNavigationEventHandler(webView_BeginNavigation);
            webView.TitleReceived += new TitleReceivedEventHandler(webView_TitleReceived);            
        }

        void webView_TitleReceived(object sender, ReceiveTitleEventArgs e)
        {
            if (Active && Container.ActivePage == this)
                Container.RaiseActivePageTitleChanged();
        }

        void webView_BeginNavigation(object sender, BeginNavigationEventArgs e)
        {
            CurrentUrl = e.Url;

            if (Active && Container.ActivePage == this)
                Container.RaiseActivePageTitleBeginNavigation();
        }

        void webView_OpenExternalLink(object sender, OpenExternalLinkEventArgs e)
        {
            Container.NewTab(e.Url);
        }

        void webView_LoadCompleted(object sender, EventArgs e)
        {
            Invalidate();

            if (Active && Container.ActivePage == this)
                Container.RaiseActivePageLoadCompleted();
        }

        internal void Invalidate()
        {
            invalidated = true;
        }

        internal void RenderUpdate()
        {
            bool disposedTexture = IsTextureDisposed;

            if (IsTextureInvalidated || Active)
            {
                bool wasCrashed = webView.IsCrashed;

                if (!wasCrashed)
                {
                    try
                    {
                        bool dirty = webView.IsDirty;

                        if (disposedTexture || Active || dirty || invalidated)
                        {
                            webView.Render().RenderTexture2D(AssociatedTexture);
                            invalidated = false;
                        }
                    }
                    catch
                    {
                        wasCrashed = true;
                    }
                }

                if (wasCrashed)
                {
                    /* Something happenned ... respawn */
                    CreateWebView(Width, Height);

                    var url = CurrentUrl;
                    CurrentUrl = String.Empty;

                    if (!String.IsNullOrEmpty(url))
                    {
                        webView.LoadURL(url);
                    }
                }
            }
        }

        public void Handle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Active && !webView.IsCrashed)
            {
                switch (e.ChangedButton)
                {
                    case System.Windows.Input.MouseButton.Left:
                        webView.InjectMouseDown(Awesomium.Core.MouseButton.Left);
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        webView.InjectMouseDown(Awesomium.Core.MouseButton.Middle);
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        webView.InjectMouseDown(Awesomium.Core.MouseButton.Right);
                        break;
                    default:
                        break;
                }
            }
        }

        public void Handle_MouseMove(Point position)
        {
            if (Active && !webView.IsCrashed)
            {
                webView.InjectMouseMove((int)position.X, (int)position.Y);
            }
        }

        public void Handle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!webView.IsCrashed)
            {
                switch (e.ChangedButton)
                {
                    case System.Windows.Input.MouseButton.Left:
                        webView.InjectMouseUp(Awesomium.Core.MouseButton.Left);
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        webView.InjectMouseUp(Awesomium.Core.MouseButton.Middle);
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        webView.InjectMouseUp(Awesomium.Core.MouseButton.Right);
                        break;
                    default:
                        break;
                }
            }
        }

        internal IntPtr Handle_WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (Active && !webView.IsCrashed)
            {
                switch ((WindowsMessage)msg)
                {
                    case WindowsMessage.KEYDOWN:
                    case WindowsMessage.KEYUP:
                    case WindowsMessage.SYSKEYDOWN:
                    case WindowsMessage.SYSKEYUP:
                    case WindowsMessage.CHAR:
                    case WindowsMessage.IME_CHAR:
                    case WindowsMessage.SYSCHAR:
                        {
                            webView.InjectKeyboardEventWin(msg, (int)wParam, (int)lParam);

                            break;
                        }
                }
            }

            return IntPtr.Zero;
        }

        internal double CurrentAngle { get; set; }

        #region Page Navigation

        public string CurrentUrl { get; private set; }

        public string Title
        {
            get
            {
                return webView.IsCrashed ? String.Empty : webView.Title;
            }
        }

        public bool Navigate(string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                if (!webView.IsCrashed)
                {
                    if (File.Exists(url))
                    {
                        if (webView.LoadFile(url))
                        {
                            CurrentUrl = url;
                            return true;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        if (webView.LoadURL(url))
                        {
                            CurrentUrl = url;
                            return true;
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;
            }
            else
                return false;
        }

        public void NavigateToFile(string path)
        {
            if (!webView.IsCrashed)
            {
                if (webView.LoadFile(path))
                    CurrentUrl = String.Empty;
            }
        }

        public void NavigateToHTML(string html)
        {
            if (!webView.IsCrashed)
            {
                if (webView.LoadHTML(html))
                    CurrentUrl = String.Empty;
            }
        }

        public bool CanGoBack
        {
            get
            {
                if (webView.IsCrashed)
                    return false;
                else
                    return webView.HistoryBackCount > 0;
            }
        }

        public bool CanGoForward
        {
            get
            {
                if (webView.IsCrashed)
                    return false;
                else
                    return webView.HistoryForwardCount > 0;
            }
        }

        public void GoBack()
        {
            if (CanGoBack)
                webView.GoBack();
        }

        public void GoForward()
        {
            if (CanGoForward)
                webView.GoForward();
        }

        public void Close()
        {
            Container.Close(this);
        }

        internal void CloseInternal()
        {
            if (!webView.IsCrashed)
                webView.Close();

            if (associatedTexture != null && !associatedTexture.IsDisposed)
                associatedTexture.Dispose();
        }

        public void Handle_MouseWheel(int p)
        {
            if (!webView.IsCrashed)
                webView.InjectMouseWheel(p);
        }

        public void Reload()
        {
            if (!webView.IsCrashed)
                webView.Reload();
        }

        #endregion
    }
}
