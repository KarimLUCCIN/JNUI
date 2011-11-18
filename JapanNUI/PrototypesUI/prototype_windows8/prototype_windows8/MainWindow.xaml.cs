using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Navigation;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace prototype_windows8
{	
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string homepage;
		private TabItem currentTab;
		private WebBrowser currentWb;

		public MainWindow()
		{
			this.InitializeComponent();
			homepage = website.Text;
			currentWb = browser;
			browser.Source = new Uri(website.Text);
			onglet.TabUI = tabControl;
		}
		
		
		private void Browser_loadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{	
			if(currentWb.CanGoBack) {
				back.IsEnabled = true;
			}
			else {
				back.IsEnabled = false;
			}
			
			if(currentWb.CanGoForward) {
				forward.IsEnabled = true;
			}
			else {
				forward.IsEnabled = false;
			}
			
			website.Text = "http://"+currentWb.Source.DnsSafeHost;
			mshtml.HTMLDocument doc = (mshtml.HTMLDocument)  currentWb.Document;

			ongletControl onglet = uniformGrid.Children[tabControl.SelectedIndex] as ongletControl;
			Grid g = onglet.Content as Grid;
			
			Button ongletButton = g.Children[1] as Button;
			ongletButton.Content = doc.title;
			ongletButton.ToolTip = doc.title;
			
			System.Windows.Controls.Image ongletImage = g.Children[0] as System.Windows.Controls.Image;
			ongletImage.ToolTip = doc.title;
			
            Dispatcher.BeginInvoke((Action)delegate
            {
                BrowserScreenshot(ongletImage);
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
		}

        private void BrowserScreenshot(System.Windows.Controls.Image ongletImage)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(currentWb);
            Bitmap image = new Bitmap((int)bounds.Width, (int)bounds.Height);
            ControlExtensions.DrawToBitmap(currentWb, image, bounds);

            System.Drawing.Image thumb = image.GetThumbnailImage((int)ongletImage.Width, (int)ongletImage.Height, null, IntPtr.Zero);

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            MemoryStream ms = new MemoryStream();
            thumb.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            ongletImage.Source = bi;
        }
		
		private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			currentTab = tabControl.SelectedItem as TabItem;
			currentWb = currentTab.Content as WebBrowser;
			
			if(currentWb.CanGoBack) {
				back.IsEnabled = true;
			}
			else {
				back.IsEnabled = false;
			}
			
			if(currentWb.CanGoForward) {
				forward.IsEnabled = true;
			}
			else {
				forward.IsEnabled = false;
			}
		}
		
		private void Back_click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				currentWb.GoBack();
			}
			catch
			{
				MessageBox.Show("Pas de page précédente", "Erreur");	
			}
		}

		private void Refresh_click(object sender, System.Windows.RoutedEventArgs e)
		{
			currentWb.Refresh();
		}

		private void Home_click(object sender, System.Windows.RoutedEventArgs e)
		{
			currentWb.Source = new Uri(homepage);
		}

		private void Forward_click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				currentWb.GoForward();
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
					currentWb.Source = new Uri(website.Text);
				}
				catch
				{
					MessageBox.Show("Mauvaise URL", "Erreur");
				}
			}
		}

		private void Plus_click(object sender, System.Windows.RoutedEventArgs e)
		{
			WebBrowser wb = new WebBrowser();
			wb.Source = new Uri(homepage);
			wb.LoadCompleted += new LoadCompletedEventHandler(Browser_loadCompleted);
			
			TabItem tab = new TabItem();
			tab.Content = wb;
			tabControl.Items.Add(tab);
			tab.Focus();
			
			ongletControl oc = new ongletControl();
			oc.TabUI = tabControl;
			oc.TabIndex = tabControl.Items.Count -1;
			uniformGrid.Children.Add(oc);
		}

		private void CloseTab_click(object sender, System.Windows.RoutedEventArgs e)
		{
		}
	}
}