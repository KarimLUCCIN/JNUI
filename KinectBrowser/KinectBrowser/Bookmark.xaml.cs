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
using System.Xml;
using System.Reflection;
using System.IO;
using KinectBrowser.D3D;

namespace KinectBrowser
{
	/// <summary>
	/// Interaction logic for Bookmark.xaml
	/// </summary>
	public partial class Bookmark : Window
	{
		private KinectBrowser.D3D.Browser.D3DBrowserTab currentBrowser;
		private XmlDocument xmlDoc;
		private String xmlPath;
		
		public Bookmark(KinectBrowser.D3D.Browser.D3DBrowserTab browser)
		{
			this.InitializeComponent();

            currentBrowser = browser;
			xmlPath = Directory.GetParent(Environment.CurrentDirectory)+"\\KinectBrowser\\bookmark.xml";
			
			xmlDoc = new XmlDocument();
			xmlDoc.Load(xmlPath);
			
			XmlNodeList bookmarkList = xmlDoc.GetElementsByTagName("Bookmark");
 
			foreach (XmlNode node in bookmarkList)
			{
				XmlElement bookmarkElement = (XmlElement) node;
 
				string title = bookmarkElement.GetElementsByTagName("Title")[0].InnerText;
				string url = bookmarkElement.GetElementsByTagName("Url")[0].InnerText;
				
				ListViewItem item = new ListViewItem();
				item.Content = title;
				item.ToolTip = url;
				item.Selected += new RoutedEventHandler(Select_bookmark);
				listView1.Items.Add(item);
			}
		}

		private void addButton_click(object sender, System.Windows.RoutedEventArgs e)
		{
			ListViewItem item = new ListViewItem();
			item.Content = titleTxt.Text;
			item.ToolTip = urlTxt.Text;
			listView1.Items.Add(item);
			
			XmlElement bookmarkElement = xmlDoc.CreateElement("Bookmark");
			
			XmlElement titleElement = xmlDoc.CreateElement("Title");
			titleElement.InnerText = titleTxt.Text;
			bookmarkElement.AppendChild(titleElement);
			
			XmlElement urlElement = xmlDoc.CreateElement("Url");
			urlElement.InnerText = urlTxt.Text;
			bookmarkElement.AppendChild(urlElement);
			
			xmlDoc["Bookmarks"].AppendChild(bookmarkElement);
			
			xmlDoc.Save(xmlPath);
			
			this.Close();
		}

		private void cancelButton_click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.Close();
		}

		private void Select_bookmark(object sender, System.Windows.RoutedEventArgs e)
		{
			ListViewItem item = e.Source as ListViewItem;
            currentBrowser.Navigate(item.ToolTip.ToString());
			this.Close();
		}
	}
}