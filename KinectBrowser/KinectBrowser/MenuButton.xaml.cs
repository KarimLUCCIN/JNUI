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
	/// Interaction logic for MenuButton.xaml
	/// </summary>
	public partial class MenuButton : UserControl
	{
		public MenuButton()
		{
			this.InitializeComponent();
		}
        
        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(MenuButton), new UIPropertyMetadata("", ButtonTextChanged));

        private static void ButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mnuButton = d as MenuButton;
            if (mnuButton != null)
            {
                mnuButton.buttonTxt.Text = mnuButton.ButtonText;
            }
        }        
	}
}