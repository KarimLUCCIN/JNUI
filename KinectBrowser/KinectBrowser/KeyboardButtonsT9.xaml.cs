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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectBrowser
{
	/// <summary>
	/// Interaction logic for KeyboardButtons.xaml
	/// </summary>
	public partial class KeyboardButtonsT9 : UserControl
	{
		public KeyboardButtonsT9()
		{
            this.InitializeComponent();
		}
		
		public KeyboardButtonsT9(String s) {
			
            this.InitializeComponent();

			center.Content = s;	
		}
        
		private void onClick(object sender, System.Windows.RoutedEventArgs e)
		{
           
		}
	}
}