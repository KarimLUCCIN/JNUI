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

namespace KinectBrowser
{
	/// <summary>
	/// Interaction logic for MenuControls.xaml
	/// </summary>
    public partial class MenuControls : UserControl
	{
        private MenuMode menuMode;

        public MenuMode MenuMode
        {
            get { return menuMode; }
            set
            {
                menuMode = value;
                UpdateTexts();
            }
        }

		public MenuControls()
		{
            this.InitializeComponent();

            TextBlock textDown = downButton.LayoutRoot.Children[1] as TextBlock;
            RotateTransform rotation = new RotateTransform(180, textDown.RenderTransformOrigin.X, textDown.RenderTransformOrigin.Y);
            textDown.RenderTransform = rotation;

            MenuMode = KinectBrowser.MenuMode.ClickPrincipal;
		}

        private void UpdateTexts()
        {
            switch (menuMode)
            {
                case MenuMode.ZoomPrincipal:
                    {
                        upButton.ButtonText = "Zoom";
                        rightButton.ButtonText = "Next Tab";
                        downButton.ButtonText = "?";
                        leftButton.ButtonText = "Previous Tab";

                        break;
                    }
                default:
                case MenuMode.ClickPrincipal:
                    {
                        upButton.ButtonText = "New Tab";
                        rightButton.ButtonText = "Scroll";
                        downButton.ButtonText = "Close Tab";
                        leftButton.ButtonText = "Click";

                        break;
                    }
            }
        }
	}
}