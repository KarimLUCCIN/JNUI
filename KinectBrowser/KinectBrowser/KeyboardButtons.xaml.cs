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
	public partial class KeyboardButtons : UserControl
	{
		public KeyboardButtons()
		{
            this.InitializeComponent();

            VisualStateManager.GoToState(this, "Open", false);
		}
		
		public KeyboardButtons(String s) {
			
            this.InitializeComponent();

			center.Content = s;
			char1.Content = s[0];
			char2.Content = s[1];
			char3.Content = s[2];
            char4.Content = s[3];

            VisualStateManager.GoToState(this, "Open", false);			
		}

		private void onClick(object sender, System.Windows.RoutedEventArgs e)
		{
			if(VisualStateGroup.CurrentState.Name.Equals("Closed"))
                VisualStateManager.GoToState(this, "Open", false);
			else
                VisualStateManager.GoToState(this, "Closed", false);
		}
	}
}