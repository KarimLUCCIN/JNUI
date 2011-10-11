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
using JapanNUI.Interaction.Gestures;
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

        public InteractionsManager Manager { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            Closed += new EventHandler(MainWindow_Closed);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            Manager.Shutdown();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
            LocationChanged += new EventHandler(MainWindow_LocationChanged);

            UpdateClientArea();

            WindowInterop = new WindowInteropHelper(this);
            WindowHandle = WindowInterop.Handle;

            Manager = new InteractionsManager(this);
            Manager.Initialize(new IInputProviderBuilder[] { new MouseProviderBuilder(), new KinectProviderBuilder() });

            var closeGestureElements = new Dictionary<string, SimpleGesture[]>();
            closeGestureElements["left"] = new SimpleGesture[]{SimpleGesture.Left, SimpleGesture.Top, SimpleGesture.Right, SimpleGesture.Bottom};
            var closeGesture = new RecognizedGesture(closeGestureElements);
            closeGesture.Activated += delegate
            {
                Close();
            };
            Manager.RecordRecognizedGesture(closeGesture);

            var maximizeMinimizeGestureElements = new Dictionary<string, SimpleGesture[]>();
            maximizeMinimizeGestureElements["left"] = new SimpleGesture[] { SimpleGesture.Left, SimpleGesture.Right };
            var maximizeMinimizeGesture = new RecognizedGesture(maximizeMinimizeGestureElements);
            maximizeMinimizeGesture.Activated += delegate
            {
                WindowState = WindowState == System.Windows.WindowState.Maximized
                    ? System.Windows.WindowState.Normal
                    : System.Windows.WindowState.Maximized;
            };
            Manager.RecordRecognizedGesture(maximizeMinimizeGesture);
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
            Manager.Update(provider);

            if (Manager.GestureSequenceManager != null)
            {
                Dispatcher.Invoke((Action)delegate
                {
                    currentGesture.Text = Manager.GestureSequenceManager.CurrentSequence.ToString();
                });
            }
        }

        public void UpdatePrimaryCursor(Vector3 position)
        {
            Dispatcher.Invoke((Action)delegate
            {
                Canvas.SetLeft(defaultCursor, position.X);
                Canvas.SetTop(defaultCursor, position.Y);
            });
        }

        public void ContextDelegateMethod(Action action)
        {
            if (action != null)
            {
                Dispatcher.Invoke(action);
            }
        }

        public void DebugDisplayBgr32DepthImage(int width, int height, byte[] convertedDepthFrame, int stride)
        {
            debugDepthImage.Source = BitmapSource.Create(
                width, height, 96, 96, PixelFormats.Bgr32, null, convertedDepthFrame, stride);
        }

        #endregion
    }
}
