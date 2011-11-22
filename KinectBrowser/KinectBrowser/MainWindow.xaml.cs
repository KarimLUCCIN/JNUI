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
            SoraEngine = new SoraEngineHost((int)ActualWidth, (int)ActualHeight);
            SoraEngine.Initialize();

            browser.Attach(SoraEngine);
			
            browser.NewTab("http://www.google.com");
            browser.NewTab("http://www.wikipedia.com");
            browser.NewTab("http://www.9gag.com");
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
				//currentWb.GoBack();
			}
			catch
			{
				MessageBox.Show("Pas de page précédente", "Erreur");	
			}
		}

		private void Refresh_click(object sender, System.Windows.RoutedEventArgs e)
		{
			//currentWb.Refresh();
		}

		private void Home_click(object sender, System.Windows.RoutedEventArgs e)
		{
			browser.ActivePage.Navigate(homepage);
		}

		private void Forward_click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				//currentWb.GoForward();
			}
			catch
			{
				MessageBox.Show("Pas de page suivante", "Erreur");	
			}
		}

		private void Go_website(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key.Equals(Key.Return)){
				try
				{
					browser.ActivePage.Navigate(websiteText.Text);
				}
				catch
				{
					MessageBox.Show("Mauvaise URL", "Erreur");
				}
			}
		}
		
		private void Bookmark_click(object sender, System.Windows.RoutedEventArgs e)
		{
			Bookmark fav = new Bookmark(browser.ActivePage);
			fav.urlTxt.Text = browser.ActivePage.CurrentUrl;
			fav.titleTxt.Text = browser.ActivePage.Title;
			fav.ShowDialog();
		}
    }
}
