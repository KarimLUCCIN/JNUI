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
	/// Interaction logic for abc1.xaml
	/// </summary>
	public partial class abc1 : UserControl
	{
		public abc1()
		{
			this.InitializeComponent();
		}

		private void bouton_click(object sender, System.Windows.RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Open", true);
		}
	}
}