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
using System.ComponentModel;

namespace JapanNUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IInputListener, INotifyPropertyChanged
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

            Manager.RecordSingleRecognizedGesture("right", 
                delegate
                {
                    currentRecognizedMovement.Text = "CircleLeft";
                },
                SimpleGesture.Left, SimpleGesture.Top, SimpleGesture.Right, SimpleGesture.Bottom);

            Manager.RecordSingleRecognizedGesture("right",
                delegate
                {
                    currentRecognizedMovement.Text = "Left And Right";
                },
                SimpleGesture.Left, SimpleGesture.Right);

            Manager.RecordSingleRecognizedGesture("right",
                delegate
                {
                    currentRecognizedMovement.Text = "Bottom Left";
                },
                SimpleGesture.Bottom, SimpleGesture.Left);

            Manager.RecordSingleRecognizedGesture("right",
                delegate
                {
                    currentRecognizedMovement.Text = "Top Left";
                },
                SimpleGesture.Top, SimpleGesture.Left);

            Manager.RecordSingleRecognizedGesture("right",
                delegate
                {
                    currentRecognizedMovement.Text = "Top Right";
                },
                SimpleGesture.Top, SimpleGesture.Right);

            Manager.RecordSingleRecognizedGesture("right",
                delegate
                {
                    currentRecognizedMovement.Text = "BottomRight And Right";
                },
                SimpleGesture.BottomRight, SimpleGesture.Right);
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
                    if (Manager.GestureSequenceManager != null && Manager.GestureSequenceManager.CurrentSequence != null)
                    {
                        currentGesture.Text = Manager.GestureSequenceManager.CurrentSequence.ToString();
                    }
                });
            }
        }

        private bool isPrimaryCursorTracked = false;

        public bool IsPrimaryCursorTracked
        {
            get { return isPrimaryCursorTracked; }
            set
            {
                if (value != isPrimaryCursorTracked)
                {
                    isPrimaryCursorTracked = value;
                    RaisePropertyChanged("IsPrimaryCursorTracked");
                }
            }
        }

        private bool isSecondaryCursorTracked = false;

        public bool IsSecondaryCursorTracked
        {
            get { return isSecondaryCursorTracked; }
            set
            {
                if (value != isSecondaryCursorTracked)
                {
                    isSecondaryCursorTracked = value;
                    RaisePropertyChanged("IsSecondaryCursorTracked");
                }
            }
        }


        public void UpdatePrimaryCursor(Vector3 position, CursorState state)
        {
            Dispatcher.Invoke((Action)delegate
            {
                IsPrimaryCursorTracked = state == CursorState.Tracked;

                Canvas.SetLeft(defaultCursor, position.X);
                Canvas.SetTop(defaultCursor, position.Y);
            });
        }

        public void UpdateSecondaryCursor(Vector3 position, CursorState state)
        {
            Dispatcher.Invoke((Action)delegate
            {
                IsSecondaryCursorTracked = state == CursorState.Tracked;

                Canvas.SetLeft(secondaryCursor, position.X);
                Canvas.SetTop(secondaryCursor, position.Y);
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

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        #endregion
    }
}
