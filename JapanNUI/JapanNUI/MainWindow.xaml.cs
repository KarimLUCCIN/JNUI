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

        public MouseProvider MouseProvider { get; private set; }
        public KinectProvider KinectProvider { get; private set; }

        public GestureSequenceManager GestureSequenceManager { get; private set; }

        List<RecognitionSequenceMachine> Tests = new List<RecognitionSequenceMachine>();

        public MainWindow()
        {
            InitializeComponent();

            Tests.AddRange(new RecognitionSequenceMachine[]{
                new RecognitionSequenceMachine(new SimpleGesture[]{SimpleGesture.Left, SimpleGesture.Top, SimpleGesture.Right, SimpleGesture.Bottom }),
                new RecognitionSequenceMachine(new SimpleGesture[]{SimpleGesture.Left, SimpleGesture.Right }),
            });

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

            GestureSequenceManager = new GestureSequenceManager(
                (from position in positionProviders select new GestureManager(position.Id)).ToArray(), TimeSpan.FromSeconds(5));
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
            if (GestureSequenceManager == null)
            {
                /* not yet initialized */
                return;
            }

            bool primary = true;

            var positions = provider.Positions;

            GestureSequenceManager.Update(positions, ClientArea);

            string gestureStr = GestureSequenceManager.CurrentSequence.ToString();

            for (int i = 0; i < positions.Length; i++)
            {
                var position = positions[i];

                if (primary)
                {
                    primary = false;
                    UpdatePrimaryCursor(position.CurrentPoint.Position);

                    break;
                }
            }

            var now = DateTime.Now;

            Dispatcher.Invoke((Action)delegate
            {
                currentGesture.Text = gestureStr;

#if(DEBUG)
                if (Keyboard.IsKeyDown(Key.O))
                {

                }
#endif
            });

            /* Maximize Test Sequence */
            var currentSequence = GestureSequenceManager.CurrentSequence;

            if ((now - currentSequence.LastModificationTime) >= TimeSpan.FromMilliseconds(500))
            {
                foreach (var item in Tests)
                {
                    item.Reset();

                    for (int i = 0; i < GestureSequenceManager.CurrentSequence.Count && !item.Valid; i++)
                    {
                        item.Update(GestureSequenceManager.CurrentSequence[GestureSequenceManager.CurrentSequence.Count - i - 1].simpleGestures[0]);
                    }
                }

                if(Tests[0].Valid && (!Tests[1].Valid || Tests[0].Score > Tests[1].Score))
                {
                    currentSequence.Reset();

                    Dispatcher.Invoke((Action)delegate
                    {
                        WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
                    });
                }
                else if(Tests[1].Valid)
                {
                    currentSequence.Reset();

                    Dispatcher.Invoke((Action)delegate
                    {
                        WindowState = System.Windows.WindowState.Normal;
                        Left = 0;
                    });
                }
            }
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
