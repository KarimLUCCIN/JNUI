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
			Grid.SetColumn(abc1, 1);
			Grid.SetRow(abc1, 0);
			
			KeyboardButtons def2 = new KeyboardButtons("def2");
			this.buttonGrid.Children.Add(def2);
			Grid.SetColumn(def2, 3);
			Grid.SetRow(def2, 0);
			
			KeyboardButtons ghi3 = new KeyboardButtons("ghi3");
			this.buttonGrid.Children.Add(ghi3);
			Grid.SetColumn(ghi3, 5);
			Grid.SetRow(ghi3, 0);
			
			KeyboardButtons jkl4 = new KeyboardButtons("jkl4");
			this.buttonGrid.Children.Add(jkl4);
			Grid.SetColumn(jkl4, 0);
			Grid.SetRow(jkl4, 1);
			
			KeyboardButtons mno5 = new KeyboardButtons("mno5");
			this.buttonGrid.Children.Add(mno5);
			Grid.SetColumn(mno5, 6);
			Grid.SetRow(mno5, 1);
			
			KeyboardButtons pqr6 = new KeyboardButtons("pqr6");
			this.buttonGrid.Children.Add(pqr6);
			Grid.SetColumn(pqr6, 0);
			Grid.SetRow(pqr6, 3);
			
			KeyboardButtons stu7 = new KeyboardButtons("stu7");
			this.buttonGrid.Children.Add(stu7);
			Grid.SetColumn(stu7, 6);
			Grid.SetRow(stu7, 3);
			
			KeyboardButtons vwx8 = new KeyboardButtons("vwx8");
			this.buttonGrid.Children.Add(vwx8);
			Grid.SetColumn(vwx8, 1);
			Grid.SetRow(vwx8, 4);
			
			KeyboardButtons yzat9 = new KeyboardButtons("yz@9");
			this.buttonGrid.Children.Add(yzat9);
			Grid.SetColumn(yzat9, 3);
			Grid.SetRow(yzat9, 4);
			
			KeyboardButtons symb = new KeyboardButtons(".*/0");
			this.buttonGrid.Children.Add(symb);
			Grid.SetColumn(symb, 5);
			Grid.SetRow(symb, 4);
		}
	}
}