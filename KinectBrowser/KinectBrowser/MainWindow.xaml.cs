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

namespace KinectBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SoraEngineHost SoraEngine { get; private set; }

		private string homepage;
        public MainWindow()
        {
            InitializeComponent();
            homepage = "http://www.google.fr";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InteractionsCore.Initialize();

            SoraEngine = new SoraEngineHost((int)browser.ActualWidth, (int)browser.ActualHeight);
            SoraEngine.Initialize();
            SoraEngine.CurrentEngine.AfterRender += new EventHandler(CurrentEngine_AfterRender);

            browser.Attach(SoraEngine);
			
            browser.NewTab("http://www.google.com");
            browser.NewTab("http://www.wikipedia.com");
            browser.NewTab("http://www.9gag.com");
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
    }
}
