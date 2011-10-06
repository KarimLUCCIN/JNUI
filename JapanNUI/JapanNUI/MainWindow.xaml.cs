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
using JapanNUI.Interaction;
using System.Windows.Interop;
using JapanNUI.Input.Mouse;
using JapanNUI.Interaction.Maths;
using JapanNUI.Input.Kinect;
using JapanNUI.Interaction.Recognition;

namespace JapanNUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IInputListener
    {
        public IntPtr WindowHandle { get; private set; }

        public WindowInteropHelper WindowInterop { get; private set; }

        public MouseProvider MouseProvider { get; private set; }
        public KinectProvider KinectProvider { get; private set; }

        public GestureManager[] GestureManagers { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            Closed += new EventHandler(MainWindow_Closed);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            MouseProvider.Shutdown();
            KinectProvider.Shutdown();
        }

        public IInputProvider CurrentProvider
        {
            get
            {
                if (KinectProvider.Available && KinectProvider.Enabled)
                    return KinectProvider;
                else
                    return MouseProvider;
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
            LocationChanged += new EventHandler(MainWindow_LocationChanged);

            UpdateClientArea();

            WindowInterop = new WindowInteropHelper(this);
            WindowHandle = WindowInterop.Handle;

            MouseProvider = new MouseProvider(this);
            MouseProvider.Enabled = false;

            KinectProvider = new KinectProvider(this);

            if (!KinectProvider.Available)
                MouseProvider.Enabled = true;

            var positionProviders = CurrentProvider.Positions;

            GestureManagers = new GestureManager[positionProviders.Length];

            for (int i = 0; i < positionProviders.Length; i++)
                GestureManagers[i] = new GestureManager();
        }

        void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            UpdateClientArea();
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateClientArea();
        }

        private void UpdateClientArea()
        {
            var origin = clientCanvas.PointToScreen(new Point(0, 0));
            var far = clientCanvas.PointToScreen(new Point(clientCanvas.ActualWidth, clientCanvas.ActualHeight));

            ClientArea = new Interaction.Maths.Rectangle(origin.X, origin.Y, far.X - origin.X, far.Y - origin.Y);
        }

        #region IInputListener Members

        public Interaction.Maths.Rectangle ScreenArea
        {
            get { return WindowUtils.GetScreenArea(this); }
        }

        public Interaction.Maths.Rectangle ClientArea { get; private set; }

        public void Update(IInputProvider provider)
        {
            bool primary = true;

            var positions = provider.Positions;

            string gestureStr = "";

            for (int i = 0; i < positions.Length; i++)
            {
                var position = positions[i];

                if (primary)
                {
                    primary = false;
                    UpdatePrimaryCursor(position.CurrentPoint.Position);
                }

                GestureManagers[i].MinimumGestureLength = 0.02 * Math.Min(ClientArea.Size.X, ClientArea.Size.Y);
                GestureManagers[i].Update(position.CurrentPoint);

                gestureStr+=GestureManagers[i].CurrentGesture;

                if(i +1 < positions.Length)
                    gestureStr += " ; ";
            }

            Dispatcher.Invoke((Action)delegate
            {
                currentGesture.Text = gestureStr;
            });
        }

        private void UpdatePrimaryCursor(Vector3 position)
        {
            Dispatcher.Invoke((Action)delegate
            {
                Canvas.SetLeft(defaultCursor, position.X);
                Canvas.SetTop(defaultCursor, position.Y);
            });
        }

        #endregion

        #region IInputListener Members


        public void DebugDisplayBgr32DepthImage(int width, int height, byte[] convertedDepthFrame, int stride)
        {
            debugDepthImage.Source = BitmapSource.Create(
                width, height, 96, 96, PixelFormats.Bgr32, null, convertedDepthFrame, stride);
        }

        #endregion
    }
}
