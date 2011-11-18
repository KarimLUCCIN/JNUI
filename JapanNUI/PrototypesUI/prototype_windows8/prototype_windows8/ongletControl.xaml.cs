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
using System.Windows.Controls.Primitives;

namespace prototype_windows8
{
	/// <summary>
	/// Interaction logic for ongletControl.xaml
	/// </summary>
	public partial class ongletControl : UserControl
	{	
		private static TabControl tab;
		
		public ongletControl()
		{
			this.InitializeComponent();
		}
		
		public TabControl TabUI
		{
			get
			{
				return (TabControl) GetValue(TabUIProperty);
			}
			set
			{
				SetValue(TabUIProperty, value);
			}
		}
		
		public static readonly DependencyProperty TabUIProperty =
  			DependencyProperty.Register(
      		"TabUI",
      		typeof(TabControl),
      		typeof(ongletControl),
     		new FrameworkPropertyMetadata(
         		new PropertyChangedCallback(ChangeTab)));
		
		private static void ChangeTab(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			tab = e.NewValue as TabControl;
		}
		
		private void Change_SelectionTab(object sender, System.Windows.RoutedEventArgs e)
		{
			tab.SelectedIndex = this.TabIndex;
		}

		private void CloseTab_click(object sender, System.Windows.RoutedEventArgs e)
		{
			if(tab.Items.Count > 1) 
			{
				UniformGrid grid = this.Parent as UniformGrid;
				TabItem delete = tab.SelectedItem as TabItem;
				
				if(tab.SelectedIndex == 0)
					tab.SelectedIndex = 1;
				else
					tab.SelectedIndex = tab.SelectedIndex - 1;
				
				tab.Items.Remove(delete);
				grid.Children.Remove(this);
				
				CorrectButtonIndex(grid);
			}
		}
		
		private void CorrectButtonIndex(UniformGrid grid) 
		{
			int x = 0;
			foreach (ongletControl b in grid.Children) 
			{
				b.TabIndex = x;
				x++;
			}
		}

		private void Change_SelectionTab(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			tab.SelectedIndex = this.TabIndex;
		}
	}
}