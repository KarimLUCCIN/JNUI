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

namespace prototype_windows8
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string homepage;
		public MainWindow()
		{
			this.InitializeComponent();
			homepage = website.Text;
			browser.Source = new Uri(website.Text);
			// Insert code required on object creation below this point.
		}
		
		private void Browser_loaded(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
			browser.Focus();
			website.Text = browser.Source.AbsoluteUri;
			mshtml.HTMLDocument doc = (mshtml.HTMLDocument) browser.Document;
			onglet.Content = doc.title;
			//MessageBox.Show(doc.title);
		}
		
		private void Back_click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				browser.GoBack();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Pas de page précédente", "Erreur");	
			}
		}

		private void Refresh_click(object sender, System.Windows.RoutedEventArgs e)
		{
			browser.Refresh();
		}

		private void Home_click(object sender, System.Windows.RoutedEventArgs e)
		{
			browser.Source = new Uri(homepage);
		}

		private void Forward_click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				browser.GoForward();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Pas de page suivante", "Erreur");	
			}
		}

		private void Go_website(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key.Equals(Key.Return)){
				browser.Source = new Uri(website.Text);
			}
		}
	}
}