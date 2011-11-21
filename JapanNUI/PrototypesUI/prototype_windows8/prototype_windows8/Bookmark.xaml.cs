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

namespace prototype_windows8
{
	/// <summary>
	/// Interaction logic for Bookmark.xaml
	/// </summary>
	public partial class Bookmark : Window
	{
		public Bookmark()
		{
			this.InitializeComponent();
			
			/*System.Xml.XmlDocument loadDoc = new System.Xml.XmlDocument();
			loadDoc.Load(@"c:\Favorites.xml");
			
			foreach (System.Xml.XmlNode favNode in loadDoc.SelectNodes("/Favorites/Item"))
			{
    			listView1.Items.Add(favNode.Attributes["url"].InnerText);
			}*/
		}

		private void addButton_click(object sender, System.Windows.RoutedEventArgs e)
		{
			ListBoxItem item = new ListBoxItem();
			item.Content = titleTxt.Text;
			item.ToolTip = urlTxt.Text;
			listBox1.Items.Add(item);
		}

		private void removeButton_click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
    			listBox1.Items.RemoveAt(listBox1.SelectedIndex);
			}
			catch
			{
    			MessageBox.Show("You need to select an item");
			}
		}

		private void saving_bookmark(object sender, System.ComponentModel.CancelEventArgs e)
		{
			/*System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(@"c:\Favorites.xml", null);

			writer.WriteStartElement("Favorites");
			for (int i = 0; i < listView1.Items.Count; i++)
			{
				ListViewItem l = listView1.Items[i] as ListViewItem;
    			writer.WriteStartElement("Item");
    			writer.WriteAttributeString("url", l.Content.ToString());
    			writer.WriteEndElement();
			}
			writer.WriteEndElement();
			writer.Close();*/
		}
	}
}