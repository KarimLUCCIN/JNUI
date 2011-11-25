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
            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
            LocationChanged += new EventHandler(MainWindow_LocationChanged);

            UpdateClientArea();

            WindowInterop = new WindowInteropHelper(this);
            WindowHandle = WindowInterop.Handle;

            InteractionsCore.Initialize();

            SoraEngine = new SoraEngineHost((int)browser.ActualWidth, (int)browser.ActualHeight);
            SoraEngine.Initialize();
            SoraEngine.CurrentEngine.AfterRender += new EventHandler(CurrentEngine_AfterRender);

            InteractionsManager = new Interaction.InteractionsManager(this);
            InteractionsManager.Initialize(new IInputProvider[] { new MouseProvider(this) });

            browser.Attach(SoraEngine);
			
            browser.NewTab("http://www.google.com");
            browser.NewTab("http://www.wikipedia.com");
            browser.NewTab("http://www.9gag.com");

            InteractionsCore.Core.Loop += new EventHandler(Core_Loop);
        }

        void Core_Loop(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                var provider = InteractionsManager.CurrentProvider;
                if (provider != null)
                {
                    var p0 = provider.Positions[0].CurrentPoint;

                    Canvas.SetLeft(mainCursor, p0.Position.X);
                    Canvas.SetTop(mainCursor, p0.Position.Y);
                }
            });
        }

        int lastRenderingDurationMs = -1;

        void CurrentEngine_AfterRender(object sender, EventArgs e)
        {
            var currentRenderingDurationMs = (int)SoraEngine.LastRenderingDuration.TotalMilliseconds;

            if (currentRenderingDurationMs != lastRenderingDurationMs)
            {
                lastRenderingDurationMs = currentRenderingDurationMs;

                Dispatcher.Invoke((Action)delegate
                {
                    statisticsLabel.Text = String.Format("Rendering time: {0}ms", (int)SoraEngine.LastRenderingDuration.TotalMilliseconds);
                });
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            browser.TabNext();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            browser.Focus();
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
