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
	internal static class NativeMethods
{
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(IntPtr hObject);
}
	
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string homepage;
		private TabItem currentTab;
		private WebBrowser currentWb;
		
		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		public static extern long BitBlt(System.IntPtr a,int b,int c,int d,int e,System.IntPtr f,int g,int h,int i);
		
		public MainWindow()
		{
			this.InitializeComponent();
			homepage = website.Text;
			browser.Source = new Uri(website.Text);
		}
		
		private void Browser_loaded(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{	
			website.Text = currentWb.Source.AbsoluteUri;
			mshtml.HTMLDocument doc = (mshtml.HTMLDocument)  currentWb.Document;

			Button currentButton = uniformGrid.Children[tabControl.SelectedIndex] as Button;
			currentButton.Content = doc.title;
			currentButton.ToolTip = doc.title;
			currentTab.Header = doc.title;
			
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
			
			Rect bounds = VisualTreeHelper.GetDescendantBounds(currentWb);
            System.Windows.Point p0 = currentWb.PointToScreen(bounds.TopLeft);  
           	System.Drawing.Point p1 = new System.Drawing.Point((int)p0.X, (int)p0.Y);  
            Bitmap image = new Bitmap((int)bounds.Width, (int)bounds.Height);  
            Graphics imgGraphics = Graphics.FromImage(image);  
            imgGraphics.CopyFromScreen(p1.X, p1.Y, 0, 0, new System.Drawing.Size((int)bounds.Width, (int)bounds.Height));  
			
			System.Drawing.Image thumb = image.GetThumbnailImage((int)image1.Width, (int)image1.Height, null, IntPtr.Zero);
			
			Graphics g2 = Graphics.FromImage(thumb);
			IntPtr dc2 = g2.GetHdc();
			BitBlt(dc2, 0, 0, (int)image1.Width, (int)image1.Height, dc2, 0, 0, 13369376);
			g2.ReleaseHdc(dc2);

			BitmapImage bi = new BitmapImage();
			bi.BeginInit();
			MemoryStream ms = new MemoryStream();
			thumb.Save(ms, ImageFormat.Bmp);
			ms.Seek(0, SeekOrigin.Begin);
			bi.StreamSource = ms; 
			bi.EndInit();
			image1.Source = bi;
			
			/*if (tabControl.Items.Count == 1) {
				Button b = tabControl.Items[0] as Button;
				
				Style style = this.FindResource("ButtonStyle2") as Style;
				
				MessageBox.Show(b.ToString());
				//Button c = style.Resources.FindName("close") as Button;
			}*/
		}
		
		public static BitmapSource ToBitmapSource(System.Drawing.Bitmap source)
   		{
        	BitmapSource bitSrc = null;

        	var hBitmap = source.GetHbitmap();

        	try
        	{
            	bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
              		hBitmap,
                	IntPtr.Zero,
                	Int32Rect.Empty,
                	BitmapSizeOptions.FromEmptyOptions());
        	}
        	catch (Win32Exception)
        	{
            	bitSrc = null;
        	}
        	finally
        	{
            	NativeMethods.DeleteObject(hBitmap);
        	}

        	return bitSrc;
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
			wb.LoadCompleted += new LoadCompletedEventHandler(Browser_loaded);
			
			TabItem tab = new TabItem();
			tab.Content = wb;
			tabControl.Items.Add(tab);
			tab.Focus();
			
			Button b = new Button();
			b.Style = this.FindResource("ButtonStyle2") as Style;
			b.Foreground = onglet.Foreground;
			b.FontSize = onglet.FontSize;
			b.Height = onglet.Height;
			b.Width = onglet.Width;
			b.Click += new RoutedEventHandler(Change_SelectionTab);
			b.TabIndex = tabControl.Items.Count - 1;
			uniformGrid.Children.Add(b);
			
			
			/*Graphics g1 = this.CreateGraphics();
			Image MyImage = new Bitmap(this.ClientRectangle.Width, this.ClientRectangle.Height, g1);
			Graphics g2 = Graphics.FromImage(MyImage);
			IntPtr dc1 = g1.GetHdc();
			IntPtr dc2 = g2.GetHdc();
			BitBlt(dc2, 0, 0, this.ClientRectangle.Width, this.ClientRectangle.Height, dc1, 0, 0, 13369376);
			g1.ReleaseHdc(dc1);
			g2.ReleaseHdc(dc2);
			MyImage.Save(@"c:\Captured.jpg", ImageFormat.Jpeg);
			MessageBox.Show("Finished Saving Image");*/
		}

		private void Change_SelectionTab(object sender, System.Windows.RoutedEventArgs e)
		{
			Button b = e.Source as Button;
			tabControl.SelectedIndex = b.TabIndex;
		}

		private void CloseTab_click(object sender, System.Windows.RoutedEventArgs e)
		{
			if(tabControl.Items.Count > 1) 
			{
				Button currentButton = uniformGrid.Children[tabControl.SelectedIndex] as Button;
				TabItem delete = tabControl.SelectedItem as TabItem;
				
				if(tabControl.SelectedIndex == 0)
					tabControl.SelectedIndex = 1;
				else
					tabControl.SelectedIndex = tabControl.SelectedIndex - 1;
				
				tabControl.Items.Remove(delete);
				uniformGrid.Children.Remove(currentButton);
				
				CorrectButtonIndex();
			}
		}
		
		private void CorrectButtonIndex() 
		{
			int x = 0;
			foreach (Button b in uniformGrid.Children) 
			{
				b.TabIndex = x;
				x++;
			}
		}
	}
}