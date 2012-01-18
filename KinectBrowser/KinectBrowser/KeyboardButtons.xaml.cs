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

            VisualStateManager.GoToState(this, "Closed", true);
		}
		
		public KeyboardButtons(String s) {
			
            this.InitializeComponent();

			center.Content = s;
			char1.Content = s[0];
			char2.Content = s[1];
			char3.Content = s[2];
            char4.Content = s[3];

            VisualStateManager.GoToState(this, "Closed", true);			
		}

        private bool closed = true;

        public bool IsClosed
        {
            get { return closed; }
            set { closed = value; }
        }

        public void OpenMenu()
        {
            if (closed)
            {
                VisualStateManager.GoToState(this, "Open", true);
                closed = false;
            }
        }

        public void CloseMenu()
        {
            if (!closed)
            {
                VisualStateManager.GoToState(this, "Closed", true);
                closed = true;
            }
        }
        
		private void onClick(object sender, System.Windows.RoutedEventArgs e)
		{
            if (IsClosed)
                OpenMenu();
            else
                CloseMenu();
		}
	}
}