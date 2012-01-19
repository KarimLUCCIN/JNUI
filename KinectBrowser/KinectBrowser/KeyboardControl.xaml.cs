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
	/// Interaction logic for KeyboardControl.xaml
	/// </summary>
	public partial class KeyboardControl : UserControl
	{
		public KeyboardControl()
		{
			this.InitializeComponent();
		}

		private void Keyboard_initialized(object sender, System.EventArgs e)
		{	
			KeyboardButtons abc1 = new KeyboardButtons("abc1");
			this.buttonGrid.Children.Add(abc1);
			
			KeyboardButtons def2 = new KeyboardButtons("def2");
			this.buttonGrid.Children.Add(def2);
			
			KeyboardButtons ghi3 = new KeyboardButtons("ghi3");
			this.buttonGrid.Children.Add(ghi3);
			
			KeyboardButtons jkl4 = new KeyboardButtons("jkl4");
			this.buttonGrid.Children.Add(jkl4);
			
			KeyboardButtons mno5 = new KeyboardButtons("mno5");
			this.buttonGrid.Children.Add(mno5);
			
			KeyboardButtons pqr6 = new KeyboardButtons("pqr6");
			this.buttonGrid.Children.Add(pqr6);
			
			KeyboardButtons stu7 = new KeyboardButtons("stu7");
			this.buttonGrid.Children.Add(stu7);
			
			KeyboardButtons vwx8 = new KeyboardButtons("vwx8");
			this.buttonGrid.Children.Add(vwx8);
			
			KeyboardButtons yzat9 = new KeyboardButtons("yz@9");
			this.buttonGrid.Children.Add(yzat9);
			
			KeyboardButtons symb = new KeyboardButtons(".*/0");
			this.buttonGrid.Children.Add(symb);
		}
	}
}