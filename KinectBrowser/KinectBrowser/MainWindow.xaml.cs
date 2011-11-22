using System;
using System.Collections.Generic;
using System.Linq;
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
using KinectBrowser.D3D;

namespace KinectBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SoraEngineHost SoraEngine { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SoraEngine = new SoraEngineHost((int)ActualWidth, (int)ActualHeight);
            SoraEngine.Initialize();

            browser.Attach(SoraEngine);

            browser.NewTab("http://www.google.com");
            browser.NewTab("http://www.wikipedia.com");
            browser.NewTab("http://www.9gag.com");
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            browser.TabNext();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            browser.Focus();
        }
    }
}
