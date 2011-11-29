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
using KinectBrowser.D3D;
using KinectBrowser.Interaction;
using KinectBrowser.Input.Mouse;
using System.Windows.Interop;
using KinectBrowser.Input.Kinect;
using KinectBrowser.Interaction.Gestures;

namespace KinectBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IInputClient
    {
        public SoraEngineHost SoraEngine { get; private set; }

        public InteractionsManager InteractionsManager { get; private set; }

        public IntPtr WindowHandle { get; private set; }

        public WindowInteropHelper WindowInterop { get; private set; }

		private string homepage;

        public MainWindow()
        {
            InitializeComponent();
            homepage = "http://www.google.fr";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Closed += new EventHandler(MainWindow_Closed);
            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
            LocationChanged += new EventHandler(MainWindow_LocationChanged);

            UpdateClientArea();

            WindowInterop = new WindowInteropHelper(this);
            WindowHandle = WindowInterop.Handle;

            InteractionsCore.Initialize();

            SoraEngine = new SoraEngineHost((int)browser.ActualWidth, (int)browser.ActualHeight);
            SoraEngine.Initialize();
            SoraEngine.CurrentEngine.AfterRender += new EventHandler(CurrentEngine_AfterRender);

            var providers = new List<IInputProvider>();
            providers.Add(new MouseProvider(this));

            if (KinectProvider.HasKinects)
                providers.Add(new KinectProvider(SoraEngine, this));

            InteractionsManager = new Interaction.InteractionsManager(this);
            InteractionsManager.Initialize(providers.ToArray());

            browser.Attach(SoraEngine);
            browser.CustomInput = true;
			
            browser.NewTab("http://www.wikipedia.com");
            browser.NewTab("http://www.youtube.com");
            browser.NewTab("http://www.google.com");

            InteractionsCore.Core.Loop += new EventHandler(Core_Loop);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            InteractionsCore.Core.Stop();
        }

        bool lastLeftButtonClickedState = false;
        bool lastRightButtonClickedState = false;

        void Core_Loop(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                if (!IsActive)
                    browser.IsActive = false;
                else
                {
                    browser.IsActive = true;

                    var provider = InteractionsManager.CurrentProvider;
                    if (provider != null)
                    {
                        var posProvider = provider.MainPosition;
                        var p0 = posProvider.CurrentPoint;

                        Canvas.SetLeft(mainCursor, p0.Position.X);
                        Canvas.SetTop(mainCursor, p0.Position.Y);

                        if (provider.GetType() != typeof(MouseProvider))
                        {
                            try
                            {
                                var absPoint = browser.PointToScreen(new Point(0, 0));
                                absPoint.X += p0.Position.X;
                                absPoint.Y += p0.Position.Y;

                                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)absPoint.X, (int)absPoint.Y);
                            }
                            catch
                            {
                                /* ignore it */
                            }
                        }

                        if (provider as KinectProvider != null)
                        {
                            UpdateKinectSpecificObjects((KinectProvider)provider);
                        }

                        if (p0.Position.Y <= browser.ActualHeight - 24)
                        {
                            browser.CustomInput_MouseMove(new Point(p0.Position.X, p0.Position.Y));

                            var leftClicked = posProvider.LeftButtonClicked;
                            var rightClicked = posProvider.RightButtonClicked;

                            if (leftClicked != lastLeftButtonClickedState)
                            {
                                var mbev = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);

                                if (leftClicked)
                                {
                                    browser.CustomInput_MouseDown(mbev);
                                }
                                else
                                {
                                    browser.CustomInput_MouseUp(mbev);
                                }
                            }

                            if (rightClicked != lastRightButtonClickedState)
                            {
                                var mbev = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right);

                                if (rightClicked)
                                {
                                    browser.CustomInput_MouseDown(mbev);
                                }
                                else
                                {
                                    browser.CustomInput_MouseUp(mbev);
                                }
                            }

                            lastLeftButtonClickedState = leftClicked;
                            lastRightButtonClickedState = rightClicked;
                        }
                    }
                }
            });
        }

        GesturePoint kinectClickPoint = new GesturePoint() { PixelMoveTreshold = 10, UpdateLatency = 0.25f, HistorySize = 10 };

        private void UpdateKinectSpecificObjects(KinectProvider provider)
        {
            bool hasValidCursor = provider.MainPosition.CurrentPoint.State == CursorState.Tracked;

            if (!hasValidCursor)
            {
                foreach (var item in contentOptionnalCanvas.Children)
                    ((Ellipse)item).Visibility = System.Windows.Visibility.Hidden;

                additionnalActionsUI.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                var clientOrigin = new Microsoft.Xna.Framework.Vector2(ClientArea.X, ClientArea.Y);

                bool hasClickPoint = false;

                if (provider.ClickBlob == null || provider.ClickBlob.Status != ImageProcessing.BlobsTracker.Status.Tracking)
                    additionnalActionsUI.Visibility = System.Windows.Visibility.Hidden;
                else
                {
                    hasClickPoint = true;

                    additionnalActionsUI.Visibility = System.Windows.Visibility.Visible;

                    var point = KinectPositionProvider.RelativePointToAbsolutePoint(provider.ClickBlob.Cursor *
                        new Microsoft.Xna.Framework.Vector2(1 / provider.KinectBlobsMatcher.DataWidth, 1 / provider.KinectBlobsMatcher.DataHeight), ClientArea) - clientOrigin;

                    kinectClickPoint.UpdatePosition(new Microsoft.Xna.Framework.Vector3(point, 0), CursorState.Tracked);

                    Canvas.SetLeft(additionnalActionsUI, kinectClickPoint.Position.X);
                    Canvas.SetTop(additionnalActionsUI, kinectClickPoint.Position.Y);
                }

                if (!hasClickPoint && (provider.Positions[0].CurrentPoint.State == CursorState.Default ||
                    provider.Positions[1].CurrentPoint.State == CursorState.Default))
                {
                    int index = 0;
                    var lst = provider.KinectBlobsMatcher.AdditionnalBlobsCursors.ToList();

                    for (index = 0; index < 4 && index < lst.Count; index++)
                    {
                        var ellipse = (Ellipse)contentOptionnalCanvas.Children[index];
                        ellipse.Visibility = System.Windows.Visibility.Visible;

                        var point = KinectPositionProvider.RelativePointToAbsolutePoint(lst[index], ClientArea) - clientOrigin;

                        Canvas.SetLeft(ellipse, point.X);
                        Canvas.SetTop(ellipse, point.Y);
                    }

                    for (; index < 4; index++)
                    {
                        ((Ellipse)contentOptionnalCanvas.Children[index]).Visibility = System.Windows.Visibility.Hidden;
                    }
                }
                else
                {
                    foreach (var item in contentOptionnalCanvas.Children)
                        ((Ellipse)item).Visibility = System.Windows.Visibility.Hidden;
                }
            }
        }

        int lastRenderingDurationMs = -1;
        int lastProcessingTime = -1;

        void CurrentEngine_AfterRender(object sender, EventArgs e)
        {
            var p_time = (InteractionsManager != null && InteractionsManager.CurrentProvider != null)
                ? (int)InteractionsManager.CurrentProvider.ProcessingTime.TotalMilliseconds
                : (int)0;

            var currentRenderingDurationMs = (int)SoraEngine.LastRenderingDuration.TotalMilliseconds;

            if (currentRenderingDurationMs != lastRenderingDurationMs || lastProcessingTime != p_time)
            {
                lastRenderingDurationMs = currentRenderingDurationMs;
                lastProcessingTime = (int)(lastProcessingTime * 0.2f + 0.7f * p_time);

                Dispatcher.Invoke((Action)delegate
                {
                    statisticsLabel.Text = String.Format("Rendering time: {0}ms\nProcessing Time: {1}ms", 
                        (int)SoraEngine.LastRenderingDuration.TotalMilliseconds,
                        lastProcessingTime);
                });
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            browser.TabNext();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            //browser.Focus();
            browser.ActivePage.Close();
        }
		
		private void Back_click(object sender, System.Windows.RoutedEventArgs e)
        {
			try
			{
                browser.GoBack();
			}
			catch
			{
				MessageBox.Show("Pas de page précédente", "Erreur");	
			}
		}

		private void Refresh_click(object sender, System.Windows.RoutedEventArgs e)
		{
            browser.Reload();
		}

		private void Home_click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (browser.ActivePage != null)
                browser.ActivePage.Navigate(homepage);
            else
                browser.NewTab(homepage);
		}

		private void Forward_click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
                browser.GoForward();
			}
			catch
			{
				MessageBox.Show("Pas de page suivante", "Erreur");	
			}
		}

		private void GoTo_keyboard(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key.Equals(Key.Return)){
				try
				{
					if (browser.ActivePage != null)
               			browser.ActivePage.Navigate(websiteText.Text);
            		else
                		browser.NewTab(homepage);
				}
				catch
				{
					MessageBox.Show("Mauvaise URL", "Erreur");
				}
			}
		}
		
		private void Bookmark_click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (browser.ActivePage != null) 
			{
				Bookmark fav = new Bookmark(browser);
				fav.urlTxt.Text = browser.ActivePage.CurrentUrl;
				fav.titleTxt.Text = browser.ActivePage.Title;
				fav.ShowDialog();
			}
			else
                browser.NewTab(homepage);
		}

		private void GoTo_click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (browser.ActivePage != null)
               	browser.ActivePage.Navigate(websiteText.Text);
            else
                browser.NewTab(homepage);
		}

        #region IInputClient Members

        void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            UpdateClientArea();
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateClientArea();
        }

        private void UpdateClientArea()
        {
            var origin = rootGrid.PointToScreen(new Point(0, 0));
            var far = rootGrid.PointToScreen(new Point(rootGrid.ActualWidth, rootGrid.ActualHeight));

            ClientArea = new Microsoft.Xna.Framework.Rectangle((int)origin.X, (int)origin.Y, (int)(far.X - origin.X), (int)(far.Y - origin.Y));
        }

        public Microsoft.Xna.Framework.Rectangle ScreenArea
        {
            get { return WindowUtils.GetScreenArea(this); }
        }

        public Microsoft.Xna.Framework.Rectangle ClientArea { get; private set; }

        #endregion
    }
}
