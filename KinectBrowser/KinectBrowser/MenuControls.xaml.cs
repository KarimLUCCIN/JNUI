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
		public MenuControls()
		{
			this.InitializeComponent();
			
			TextBlock textUp = upButton.LayoutRoot.Children[1] as TextBlock;
			textUp.Text = "New Tab";
			
			TextBlock textRight = rightButton.LayoutRoot.Children[1] as TextBlock;
			textRight.Text = "Right";
			
			TextBlock textDown = downButton.LayoutRoot.Children[1] as TextBlock;
            textDown.Text = "Close Tab";
			RotateTransform rotation = new RotateTransform(180, textDown.RenderTransformOrigin.X, textDown.RenderTransformOrigin.Y);
			textDown.RenderTransform = rotation;
			
			TextBlock textLeft = leftButton.LayoutRoot.Children[1] as TextBlock;
			textLeft.Text = "Click";
		}

		private void upButtonClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//MessageBox.Show("Up button click");
			//this.Close();
		}

		private void rightButtonClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//MessageBox.Show("Right button click");
			//this.Close();
		}

		private void downButtonClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//MessageBox.Show("Down button click");
			//this.Close();
		}

		private void leftButtonClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//MessageBox.Show("Left button click");
			//this.Close();
		}
	}
}