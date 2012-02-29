//#define DISPLAY_KINECT_SKELETON_HANDS

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
using KinectBrowser.Interaction;
using KinectBrowser.Input.Mouse;
using System.Windows.Interop;
using KinectBrowser.Input.Kinect;
using KinectBrowser.Interaction.Gestures;
using KinectBrowser.ImageProcessing;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using KinectBrowser.Controls;

namespace KinectBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IInputClient
    {
        public SoraEngineHost SoraEngine { get; private set; }

        public InteractionsManager InteractionsManager { get; private set; }

        public KinectGesturesTracker KinectGesturesTracker { get; private set; }

        public IntPtr WindowHandle { get; private set; }

        public WindowInteropHelper WindowInterop { get; private set; }

		private string homepage;

        public MainWindow()
        {
            InitializeComponent();
            homepage = "http://www.google.fr";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Closed += new EventHandler(MainWindow_Closed);
            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
            LocationChanged += new EventHandler(MainWindow_LocationChanged);

            UpdateClientArea();

            InitializeCore();

            browser.Attach(SoraEngine);
            browser.CustomInput = true;

            browser.NewTab("http://www.wikipedia.com");
            //browser.NewTab("http://www.youtube.com");
            browser.NewTab("http://www.google.com");

            //browser.NewTab("file://C|/Users/Audrey/Downloads/html/Latin%20Union%20-%20Wikipedia,%20the%20free%20encyclopedia.htm");
            //browser.NewTab("file://C|/Users/Audrey/Downloads/html/Latin%20Union%20-%20Wikipedia,%20the%20free%20encyclopedia.htm");
            //browser.NewTab("file://C|/Users/Audrey/Downloads/html/Latin%20Union%20-%20Wikipedia,%20the%20free%20encyclopedia.htm");

            InteractionsCore.Core.Loop += new EventHandler(Core_Loop);

            InitializeKeyboardActions();
        }

        private void DemoOpenURI(string p)
        {
            p = "file:///" + p.Replace(":", "|").Replace("\\", "/");
            browser.NewTab(p);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            InteractionsCore.Core.Stop();
        }

        bool lastLeftButtonClickedState = false;
        bool lastRightButtonClickedState = false;

        private void Left_ClickAction_Right()
        {
            switch (additionnalActionsUIControls.MenuMode)
            {
                case MenuMode.ZoomPrincipal:
                    browser.TabPrev();
                    hasValidatedClickAction = false;
                    break;
                case MenuMode.ClickPrincipal:
                    kinectClickAction = SpacialKinectClickAction.Scroll;
                    break;
            }
        }

        private void Left_ClickAction_Left()
        {
            switch (additionnalActionsUIControls.MenuMode)
            {
                case MenuMode.ZoomPrincipal:
                    browser.TabNext();
                    hasValidatedClickAction = false;
                    break;
                case MenuMode.ClickPrincipal:
                    var hitResult = controlsGrid.InputHitTest(Mouse.GetPosition(controlsGrid)) as DependencyObject;
                    if (hitResult != null)
                    {
                        VirtualClick(hitResult);
                    }
                    else
                        kinectClickAction = SpacialKinectClickAction.Click;
                    break;
            }
        }

        private void VirtualClick(DependencyObject hitResult)
        {
            if (hitResult == null)
                return;

            var btn = hitResult as Button;

            if (btn != null)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(btn);

                IInvokeProvider invokeProv =
                  peer.GetPattern(PatternInterface.Invoke)
                  as IInvokeProvider;

                invokeProv.Invoke();
            }
            else
            {
                var parent = VisualTreeHelper.GetParent(hitResult);
                if (parent != null)
                    VirtualClick(parent);
            }
        }

        private void Left_ClickAction_Bottom()
        {
            switch (additionnalActionsUIControls.MenuMode)
            {
                case MenuMode.ZoomPrincipal:
                    BeginKeyboard();
                    break;
                case MenuMode.ClickPrincipal:
                    if (browser.ActivePage != null)
                        browser.ActivePage.Close();
                    break;
            }
        }

        private void Left_ClickAction_Top()
        {
            switch (additionnalActionsUIControls.MenuMode)
            {
                case MenuMode.ZoomPrincipal:
                    kinectClickAction = SpacialKinectClickAction.Zoom;
                    break;
                case MenuMode.ClickPrincipal:
                    browser.NewTab("http://www.google.com");
                    break;
            }
        }

        private static int absMod(int v, int m)
        {
            return ((v % m) + m) % m;
        }
                
        private void Right_ClickAction_Right()
        {
            additionnalActionsUIControls.MenuMode = (MenuMode)absMod((int)additionnalActionsUIControls.MenuMode + 1, (int)MenuMode.Count);
        }

        private void Right_ClickAction_Left()
        {
            additionnalActionsUIControls.MenuMode = (MenuMode)absMod((int)additionnalActionsUIControls.MenuMode - 1, (int)MenuMode.Count);
        }

        private void Right_ClickAction_Bottom()
        {

        }

        private void Right_ClickAction_Top()
        {

        }

        #region Core

        private void InitializeCore()
        {
            WindowInterop = new WindowInteropHelper(this);
            WindowHandle = WindowInterop.Handle;

            InteractionsCore.Initialize();

            SoraEngine = new SoraEngineHost((int)browser.ActualWidth, (int)browser.ActualHeight);
            SoraEngine.Initialize();
            SoraEngine.CurrentEngine.AfterRender += new EventHandler(CurrentEngine_AfterRender);

            var providers = new List<IInputProvider>();
            providers.Add(new MouseProvider(this));

            if (KinectProvider.HasKinects)
                providers.Add(new KinectProvider(SoraEngine, this));

            InteractionsManager = new Interaction.InteractionsManager(this);
            InteractionsManager.Initialize(providers.ToArray());

            RegisterKinectGestures();
        }

        public bool IsKinectEnabled
        {
            get
            {
                return InteractionsManager.CurrentProvider as KinectProvider != null;
            }
        }

        Point lastCursorPoint = new Point();

        void Core_Loop(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)delegate
            {
                if (!IsActive)
                    browser.IsActive = false;
                else
                {
                    browser.IsActive = true;

                    if (SoraEngine.CurrentEngine.InputManager.IsNewKeyPress(Microsoft.Xna.Framework.Input.Keys.F6))
                    {
                        SwitchMeasurementMode();
                    }

                    if (currentMeasurementMode != MeasurementMode.None)
                        MeasurementModeUpdate();

                    var provider = InteractionsManager.CurrentProvider;

                    if (provider != null)
                    {
                        if (provider is MousePositionProvider)
                            cursorIsAlive = true;

                        var posProvider = provider.MainPosition;
                        var p0 = posProvider.CurrentPoint;

                        Canvas.SetLeft(leftCursor, p0.Position.X);
                        Canvas.SetTop(leftCursor, p0.Position.Y);

                        rightCursor.Visibility = System.Windows.Visibility.Hidden;

                        if (cursorIsAlive && (provider as KinectProvider) != null)
                        {
                            try
                            {
                                var absPoint = browser.PointToScreen(new Point(0, 0));
                                absPoint.X += p0.Position.X;
                                absPoint.Y += p0.Position.Y;

                                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)absPoint.X, (int)absPoint.Y);

                                if (kinectClickAction == SpacialKinectClickAction.Scroll)
                                {
                                    var dist = new Point(absPoint.X - lastCursorPoint.X, absPoint.Y - lastCursorPoint.Y);

                                    browser.Scroll((int)dist.X * 10, (int)dist.Y * 10);
                                }
                                else if (kinectClickAction == SpacialKinectClickAction.Zoom)
                                {
                                    var page = browser.ActivePage;
                                    if (page != null)
                                    {
                                        var dist = new Point(absPoint.X - lastCursorPoint.X, absPoint.Y - lastCursorPoint.Y);

                                        page.Zoom += (int)((dist.X + dist.Y) * 0.5f);
                                    }
                                }

                                lastCursorPoint = absPoint;
                            }
                            catch
                            {
                                /* ignore it */
                            }
                        }

                        if (provider as KinectProvider != null)
                        {
                            UpdateKinectSpecificObjects((KinectProvider)provider);
                        }

                        if (cursorIsAlive && p0.Position.Y <= browser.ActualHeight - BrowserMargin)
                        {
                            browser.CustomInput_MouseMove(new Point(p0.Position.X, p0.Position.Y));

                            var leftClicked = IsKinectEnabled ? kinectClickAction == SpacialKinectClickAction.Click : posProvider.LeftButtonClicked;
                            var rightClicked = IsKinectEnabled ? false : posProvider.RightButtonClicked; /* pas de click droit en l'état */

                            if (leftClicked != lastLeftButtonClickedState)
                            {
                                var mbev = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);

                                if (leftClicked)
                                {
                                    browser.CustomInput_MouseDown(mbev);
                                }
                                else
                                {
                                    browser.CustomInput_MouseUp(mbev);
                                }
                            }

                            if (rightClicked != lastRightButtonClickedState)
                            {
                                var mbev = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right);

                                if (rightClicked)
                                {
                                    browser.CustomInput_MouseDown(mbev);
                                }
                                else
                                {
                                    browser.CustomInput_MouseUp(mbev);
                                }
                            }

                            lastLeftButtonClickedState = leftClicked;
                            lastRightButtonClickedState = rightClicked;

                            //if (kinectClickAction == SpacialKinectClickAction.Click)
                            //    kinectClickAction = SpacialKinectClickAction.None;
                        }
                    }
                }
            });
        }

        #endregion

        #region Measurements

        public enum MeasurementMode
        {
            None,
            Precision,

            Max=Precision
        }

        MeasurementMode currentMeasurementMode = MeasurementMode.None;

        public void ResetMeasurementControlsVisibility()
        {
            measurementsPrecisionControl.Visibility = System.Windows.Visibility.Hidden;
            browser.Visibility = System.Windows.Visibility.Visible;
        }

        private void MeasurementModeUpdate()
        {
            switch (currentMeasurementMode)
            {
                default:
                case MeasurementMode.None:
                    break;
                case MeasurementMode.Precision:
                    {
                        if (!measurementsPrecisionControl.Update())
                            SwitchMeasurementMode(MeasurementMode.None);

                        break;
                    }
            }
        }

        DateTime lastMeasureModeChange = DateTime.Now;
        TimeSpan measurementTimeSwitch = TimeSpan.FromSeconds(1.5);

        private void SwitchMeasurementMode(MeasurementMode? mode = null)
        {
            if (DateTime.Now - lastMeasureModeChange < measurementTimeSwitch)
                return;
            else
            {
                lastMeasureModeChange = DateTime.Now;
            }

            switch (currentMeasurementMode)
            {
                default:
                case MeasurementMode.None:
                    break;
                case MeasurementMode.Precision:
                    {
                        measurementsPrecisionControl.ReportResults();
                        break;
                    }
            }

            ResetMeasurementControlsVisibility();

            currentMeasurementMode = mode == null ? (MeasurementMode)(((int)currentMeasurementMode + 1) % ((int)MeasurementMode.Max + 1)) : mode.Value;

            switch (currentMeasurementMode)
            {
                default:
                case MeasurementMode.None:
                    break;
                case MeasurementMode.Precision:
                    {
                        browser.Visibility = System.Windows.Visibility.Hidden;
                        measurementsPrecisionControl.Visibility = System.Windows.Visibility.Visible;

                        measurementsPrecisionControl.Begin();

                        break;
                    }
            }
        }

        #endregion

        #region Virtual Keyboard Management

        private void InitializeKeyboardActions()
        {
            virtualKeyboard.cancel.Click += new RoutedEventHandler(cancel_Click);
            virtualKeyboard.lineBreak.Click += delegate { virtualKeyboard.inputUser.Text += "\n"; };
            virtualKeyboard.back.Click += delegate
            {
                if (!String.IsNullOrEmpty(virtualKeyboard.inputUser.Text))
                {
                    virtualKeyboard.inputUser.Text = virtualKeyboard.inputUser.Text.Substring(0, virtualKeyboard.inputUser.Text.Length - 1);

                    virtualKeyboard.UpdateSuggestions();
                }
            };
            virtualKeyboard.space.Click += delegate
            {
                virtualKeyboard.inputUser.Text += " ";

                virtualKeyboard.UpdateSuggestions();
            };
            virtualKeyboard.enter.Click +=new RoutedEventHandler(enter_Click);
        }

        void enter_Click(object sender, RoutedEventArgs e)
        {
            var textInput = virtualKeyboard.inputUser.Text;
            virtualKeyboard.inputUser.Text = String.Empty;

            EndKeyboard();

            var page = browser.ActivePage;

            if (page != null)
            {
                page.SendText(textInput);
            }
        }

        void cancel_Click(object sender, RoutedEventArgs e)
        {
            EndKeyboard();
        }

        private bool isKeyboardActive = false;

        public bool IsKeyboardActive
        {
            get { return isKeyboardActive; }
            set { isKeyboardActive = value; }
        }
                
        private void BeginKeyboard()
        {
            hasValidatedClickAction = false;
            virtualKeyboard.Visibility = System.Windows.Visibility.Visible;
            isKeyboardActive = true;
            virtualKeyboard.inputUser.Text = String.Empty;
            virtualKeyboard.ClearSuggestions();
        }

        private void EndKeyboard()
        {
            LeaveKeyboardButtonsHost();
            virtualKeyboard.Visibility = System.Windows.Visibility.Collapsed;
            isKeyboardActive = false;
        }

        KeyboardButtons currentKeyboardButtonsHost = null;

        private void EnterKeyboardButtonsHost(KeyboardButtons buttonsHost)
        {
            if (buttonsHost != null && buttonsHost != currentKeyboardButtonsHost)
            {
                var parent = buttonsHost.Parent as Panel;

                if (parent != null)
                {
                    foreach (var child in parent.Children)
                    {
                        Panel.SetZIndex((UIElement)child, 0);
                    }

                    Panel.SetZIndex(buttonsHost, 1);
                }

                if (currentKeyboardButtonsHost != null)
                    LeaveKeyboardButtonsHost();

                currentKeyboardButtonsHost = buttonsHost;
                currentKeyboardButtonsHost.OpenMenu();
            }
        }

        private void LeaveKeyboardButtonsHost()
        {
            if(currentKeyboardButtonsHost != null)
            {
                currentKeyboardButtonsHost.CloseMenu();
                currentKeyboardButtonsHost = null;
                currentKeyboardButton = null;
            }
        }

        Button currentKeyboardButton = null;
        DateTime lastButtonChangeTime = DateTime.Now;

        TimeSpan keyboardClickLatency = TimeSpan.FromMilliseconds(1000);

        private void KeyboardCursorMoveHandler(Microsoft.Xna.Framework.Vector3 pos)
        {
            /* On cherche ce qui se trouve sous la souris */
            var hitResult = virtualKeyboard.InputHitTest(Mouse.GetPosition(virtualKeyboard)) as DependencyObject;
            if (hitResult != null)
            {
                KeyboardButtons btnHost = null;
                Button btn = null;
                ListBoxItem sugItem = null;

                while (hitResult != null && (btnHost == null || btn == null || sugItem == null))
                {
                    if (btnHost == null)
                        btnHost = hitResult as KeyboardButtons;

                    /* on est arrivé au contrôle */
                    if (btnHost != null)
                        break;

                    /* Peut être un bouton ? */
                    if (btn == null)
                        btn = hitResult as Button;

                    /* Item pour les suggestions */
                    if (sugItem == null)
                        sugItem = hitResult as ListBoxItem;

                    hitResult = hitResult is Run ? null : VisualTreeHelper.GetParent(hitResult);
                }

                if (btnHost != null)
                {
                    EnterKeyboardButtonsHost(btnHost);
                }

                if (currentKeyboardButton != btn)
                {
                    currentKeyboardButton = btn;
                    lastButtonChangeTime = DateTime.Now;
                }
                else
                {
                    if (sugItem != null && DateTime.Now - lastButtonChangeTime >= TimeSpan.FromSeconds(keyboardClickLatency.TotalSeconds * 6))
                    {
                        virtualKeyboard.inputUser.Text = sugItem.Content.ToString();

                        lastButtonChangeTime = DateTime.Now;
                    }
                    else if (currentKeyboardButton != null && DateTime.Now - lastButtonChangeTime >= keyboardClickLatency)
                    {
                        var txtInput = btn as TextInputButton;

                        if (txtInput == null || txtInput.Content == null)
                        {
                            VirtualClick(btn);

                            lastButtonChangeTime = DateTime.Now;
                        }
                        else
                        {
                            /* délais plus long */
                            if (DateTime.Now - lastButtonChangeTime >= TimeSpan.FromMilliseconds(keyboardClickLatency.Milliseconds * 1.5f))
                            {
                                virtualKeyboard.inputUser.Text += (txtInput.Content).ToString();

                                if (!String.IsNullOrEmpty(virtualKeyboard.inputUser.Text))
                                {
                                    virtualKeyboard.UpdateSuggestions();
                                }

                                lastButtonChangeTime = DateTime.Now;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Kinect Specifics

        object forced_actions_lock = new object();

        private void RegisterKinectGestures()
        {
            //KinectGesturesTracker = new Input.Kinect.KinectGesturesTracker();
            //KinectGesturesTracker.RecordSingleRecognizedGesture(delegate
            //{
            //    Dispatcher.Invoke((Action)delegate
            //    {
            //        browser.NewTab("http://www.google.com");
            //    });
            //}, SimpleGesture.Left, SimpleGesture.Top, SimpleGesture.Right);

            //KinectGesturesTracker.RecordSingleRecognizedGesture(delegate
            //{
            //    Dispatcher.Invoke((Action)delegate
            //    {
            //        if (browser.ActivePage != null)
            //            browser.ActivePage.Close();
            //    });
            //}, SimpleGesture.Bottom, SimpleGesture.Top, SimpleGesture.Right);

            KinectGesturesTracker = new Input.Kinect.KinectGesturesTracker();
            KinectGesturesTracker.RecordSingleRecognizedGesture((RecognizedGestureEventHandler)delegate(object m_sender, RecognizedGestureEventArgs m_e)
            {
                lock (forced_actions_lock)
                {
                    if (!cursorIsAlive)
                    {
                        kinectForcedAction_CursorFocusChangeTarget = m_e.Origin;
                        kinectForcedAction = SpecialKinectForcedAction.CursorFocusChange;
                    }
                }
            }, SimpleGesture.Left, SimpleGesture.Right, SimpleGesture.Left);

            //KinectGesturesTracker.RecordSingleRecognizedGesture((RecognizedGestureEventHandler)delegate(object m_sender, RecognizedGestureEventArgs m_e)
            //{
            //    lock (forced_actions_lock)
            //    {
            //        if (cursorIsAlive && m_e.Origin == kinectCursorBlob)
            //        {
            //            kinectForcedAction_CursorFocusChangeTarget = null;
            //            kinectForcedAction = SpecialKinectForcedAction.CursorFocusChange;
            //        }
            //    }
            //}, SimpleGesture.Left, SimpleGesture.Top, SimpleGesture.Right, SimpleGesture.Bottom);
        }

        private int BrowserMargin
        {
            get
            {
                return 24;
            }
        }

        GesturePoint kinectClickGesturePoint = new GesturePoint() { PixelMoveTreshold = 5, UpdateLatency = 0.25f, HistorySize = 0 };

        Microsoft.Xna.Framework.Vector2 kinectClickLeftBeginPosition = Microsoft.Xna.Framework.Vector2.Zero;
        Microsoft.Xna.Framework.Vector2 kinectClickRightBeginPosition = Microsoft.Xna.Framework.Vector2.Zero;

        List<BlobsTracker.TrackedBlob> kinectBlobs = new List<BlobsTracker.TrackedBlob>();

        BlobsTracker.TrackedBlob kinectCursorBlob = null;

        SpecialKinectForcedAction kinectForcedAction = SpecialKinectForcedAction.None;
        SpacialKinectClickAction kinectClickAction = SpacialKinectClickAction.None;

        BlobsTracker.TrackedBlob kinectForcedAction_CursorFocusChangeTarget = null;

        DateTime lastForcedActionTime = DateTime.Now;
        TimeSpan forcedActionLatency = TimeSpan.FromSeconds(1);

        bool cursorIsAlive = false;

        bool isWaitingForClickAction = false;
        bool hasValidatedClickAction = false;
        DateTime waitinForClickActionTime = DateTime.MinValue;
        TimeSpan waitingForClickActionLatency = TimeSpan.FromMilliseconds(500);
        
        private void UpdateKinectSpecificObjects(KinectProvider provider)
        {
#if(DISPLAY_KINECT_SKELETON_HANDS)
            skeletonCanvas.Visibility = System.Windows.Visibility.Visible;

            skeletonLeft.Visibility = provider.LeftSkeleton != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            skeletonRight.Visibility = provider.RightSkeleton != null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

            skeletonLeft.Fill = skeletonRight.Fill = Brushes.Red;
            skeletonLeft.Width = skeletonRight.Width = skeletonLeft.Height = skeletonRight.Height = 32;

            if (skeletonLeft.Visibility == System.Windows.Visibility.Visible)
            {
                Canvas.SetLeft(skeletonLeft, provider.LeftSkeleton.Value.X);
                Canvas.SetTop(skeletonLeft, provider.LeftSkeleton.Value.Y);
            }
            if (skeletonRight.Visibility == System.Windows.Visibility.Visible)
            {
                Canvas.SetLeft(skeletonRight, provider.RightSkeleton.Value.X);
                Canvas.SetTop(skeletonRight, provider.RightSkeleton.Value.Y);
            }
#endif

            if (kinectClickAction == SpacialKinectClickAction.Click)
            {

            }
            browser.DebugTextureQuad.Texture = provider.ImageProcessingEngine.grownRegions;

            var clientOrigin = new Microsoft.Xna.Framework.Vector2(ClientArea.X, ClientArea.Y);

            kinectCursorBlob = null;

            kinectBlobs.Clear();

            bool isNewClick = additionnalActionsUI.Visibility == System.Windows.Visibility.Hidden;

            bool hasValidCursor = provider.MainPosition.CurrentPoint.State == CursorState.Tracked;

            kinectCursorBlob = provider.MainBlob;

            kinectBlobs.AddRange(provider.KinectBlobsMatcher.BlobsTracker.TrackedBlobs);

            KinectGesturesTracker.Update(kinectBlobs);

            lock (forced_actions_lock)
            {
                if (kinectForcedAction != SpecialKinectForcedAction.None)
                {
                    if ((DateTime.Now - lastForcedActionTime) >= forcedActionLatency)
                    {
                        switch (kinectForcedAction)
                        {
                            case SpecialKinectForcedAction.CursorFocusChange:
                                {
                                    if (!hasValidCursor)
                                    {
                                        provider.ForceCursorAquire(kinectForcedAction_CursorFocusChangeTarget);
                                        cursorIsAlive = true;
                                    }
                                    else
                                    {
                                        provider.ForceCursorRelease();
                                        cursorIsAlive = false;
                                    }

                                    hasValidCursor = provider.MainPosition.CurrentPoint.State == CursorState.Tracked;

                                    break;
                                }
                        }
                    }

                    lastForcedActionTime = DateTime.Now;

                    kinectForcedAction = SpecialKinectForcedAction.None;
                }
            }

            if (!cursorIsAlive)
            {
                provider.ForceCursorRelease();
                hasValidCursor = false;
                isNewClick = false;
            }

            //if (hasValidCursor)
            //{
            //    if (!wasHandCrossed && provider.MainBlob.Crossed)
            //    {
            //        provider.SwitchMainBlobWitchCrossedOne();
            //        wasHandCrossed = true;
            //    }
            //    else if (wasHandCrossed && !provider.MainBlob.Crossed)
            //        wasHandCrossed = false;
            //}

            if (!cursorIsAlive)
            {
                Cursor = Cursors.None;

                leftCursor.Visibility = System.Windows.Visibility.Hidden;
                rightCursor.Visibility = System.Windows.Visibility.Hidden;

                foreach (var item in contentOptionnalCanvas.Children)
                    ((Ellipse)item).Visibility = System.Windows.Visibility.Visible;
                
                DisplayOptionnals(provider, clientOrigin);

                additionnalActionsUI.Visibility = System.Windows.Visibility.Hidden;
                additionnalActionsUIControls.Visibility = System.Windows.Visibility.Hidden;

                isWaitingForClickAction = false;
                hasValidatedClickAction = false;

                kinectClickAction = SpacialKinectClickAction.None;
            }
            else
            {
                if (!hasValidCursor)
                {
                    Cursor = Cursors.None;
                    leftCursor.Visibility = System.Windows.Visibility.Visible;
                    rightCursor.Visibility = System.Windows.Visibility.Visible;

                    Canvas.SetLeft(leftCursor, provider.Positions[0].CurrentPoint.Position.X);
                    Canvas.SetTop(leftCursor, provider.Positions[0].CurrentPoint.Position.Y);

                    Canvas.SetLeft(rightCursor, provider.Positions[1].CurrentPoint.Position.X);
                    Canvas.SetTop(rightCursor, provider.Positions[1].CurrentPoint.Position.Y);

                    foreach (var item in contentOptionnalCanvas.Children)
                        ((Ellipse)item).Visibility = System.Windows.Visibility.Visible;

                    DisplayOptionnals(provider, clientOrigin);

                    additionnalActionsUI.Visibility = System.Windows.Visibility.Hidden;
                    additionnalActionsUIControls.Visibility = System.Windows.Visibility.Hidden;

                    kinectClickAction = SpacialKinectClickAction.None;
                }
                else
                {
                    if (provider.MainBlob != null)
                    {
                        provider.MainBlob.IsHighPriority = true;
                    }

                    foreach (var item in contentOptionnalCanvas.Children)
                        ((Ellipse)item).Visibility = System.Windows.Visibility.Hidden;

                    Cursor = Cursors.Arrow;
                    leftCursor.Visibility = rightCursor.Visibility = System.Windows.Visibility.Hidden;

                    bool hasClickPoint = false;

                    if (provider.ClickBlob == null ||
                        provider.ClickBlob.Status == ImageProcessing.BlobsTracker.Status.Lost ||
                        (provider.ClickBlob.Status == ImageProcessing.BlobsTracker.Status.Waiting && provider.ClickBlob.WaitingCycles > 10))
                    {
                        additionnalActionsUI.Visibility = System.Windows.Visibility.Hidden;
                        additionnalActionsUIControls.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                    {
                        hasClickPoint = true;

                        additionnalActionsUI.Visibility = System.Windows.Visibility.Visible;
                        //KinectGesturesTracker
                        /* Sinon, on peut se manger des NaN vu que la position du blob n'est pas défnie */
                        if (provider.ClickBlob.Status == ImageProcessing.BlobsTracker.Status.Tracking)
                        {
                            var point = KinectPositionProvider.RelativePointToAbsolutePoint(provider.ClickBlob.LinearCursor *
                                new Microsoft.Xna.Framework.Vector2(1 / provider.KinectBlobsMatcher.DataWidth, 1 / provider.KinectBlobsMatcher.DataHeight), ClientArea) - clientOrigin;
                            
                            kinectClickGesturePoint.UpdatePosition(new Microsoft.Xna.Framework.Vector3(point, 0), CursorState.Tracked);

                            if (isNewClick)
                            {
                                kinectClickLeftBeginPosition = point;
                                kinectClickRightBeginPosition = XY(provider.MainPosition.CurrentPoint.Position);
                            }
                        }
                    }

                    if (!hasClickPoint && (provider.Positions[0].CurrentPoint.State == CursorState.Default ||
                        provider.Positions[1].CurrentPoint.State == CursorState.Default))
                    {
                        DisplayOptionnals(provider, clientOrigin);
                    }
                    else
                    {
                        foreach (var item in contentOptionnalCanvas.Children)
                            ((Ellipse)item).Visibility = System.Windows.Visibility.Hidden;
                    }

                    if (!hasClickPoint)
                        kinectClickAction = SpacialKinectClickAction.None;
                }

                isNewClick = isNewClick && additionnalActionsUI.Visibility == System.Windows.Visibility.Visible;

                if (isKeyboardActive)
                {
                    additionnalActionsUI.Visibility = System.Windows.Visibility.Hidden;
                    additionnalActionsUIControls.Opacity = 0;

                    KeyboardCursorMoveHandler(provider.MainPosition.CurrentPoint.Position);
                }
                else
                {

                    if (additionnalActionsUI.Visibility == System.Windows.Visibility.Visible)
                        additionnalActionsUIControls.Opacity = hasValidatedClickAction ? 0.2 : 1;

                    if (isNewClick)
                    {
                        additionnalActionsUIControls.MenuMode = MenuMode.ClickPrincipal;

                        KinectLeftClickBegin(provider, provider.MainPosition.CurrentPoint.Position);
                        isWaitingForClickAction = false;
                        hasValidatedClickAction = false;
                    }
                    else if (additionnalActionsUI.Visibility == System.Windows.Visibility.Visible)
                    {
                        if (!isWaitingForClickAction)
                        {
                            hasValidatedClickAction = false;
                            isWaitingForClickAction = true;
                            waitinForClickActionTime = DateTime.Now;
                        }
                        else
                        {
                            var now = DateTime.Now;

                            if ((now - waitinForClickActionTime) >= waitingForClickActionLatency)
                            {
                                //HandleLeftMenuAction(kinectClickGesturePoint.Position, kinectClickLeftBeginPosition);
                                HandleLeftMenuAction(kinectClickGesturePoint.Position, kinectClickLeftBeginPosition);

                                HandleRightMenuAction(provider.MainPosition.CurrentPoint.Position, kinectClickRightBeginPosition);
                            }
                            else
                            {
                                kinectClickLeftBeginPosition = new Microsoft.Xna.Framework.Vector2(kinectClickGesturePoint.Position.X, kinectClickGesturePoint.Position.Y);
                                kinectClickRightBeginPosition = XY(provider.MainPosition.CurrentPoint.Position);
                            }
                        }
                    }

                }
            }
        }
        
        private Microsoft.Xna.Framework.Vector2 DisplayOptionnals(KinectProvider provider, Microsoft.Xna.Framework.Vector2 clientOrigin)
        {
            int index = 0;
            var lst = provider.KinectBlobsMatcher.AdditionnalBlobsCursors.ToList();

            for (index = 0; index < 4 && index < lst.Count; index++)
            {
                var ellipse = (Ellipse)contentOptionnalCanvas.Children[index];
                ellipse.Visibility = System.Windows.Visibility.Visible;

                var point = KinectPositionProvider.RelativePointToAbsolutePoint(lst[index], ClientArea) - clientOrigin;

                Canvas.SetLeft(ellipse, point.X);
                Canvas.SetTop(ellipse, point.Y);
            }

            for (; index < 4; index++)
            {
                ((Ellipse)contentOptionnalCanvas.Children[index]).Visibility = System.Windows.Visibility.Hidden;
            }
            return clientOrigin;
        }

        private static Microsoft.Xna.Framework.Vector2 XY(Microsoft.Xna.Framework.Vector3 v3)
        {
            return new Microsoft.Xna.Framework.Vector2(v3.X, v3.Y);
        }

        const float menuActionPixelsTreshold = 80;

        /* pour faire un cooldown le temps de replacer la main */
        DateTime lastRightMenuAction = DateTime.Now;
        TimeSpan rightMenuActionCooldownDuration = TimeSpan.FromMilliseconds(600);

        private void HandleRightMenuAction(Microsoft.Xna.Framework.Vector3 currentPosition, Microsoft.Xna.Framework.Vector2 clickPosition)
        {
            var pos2d = XY(currentPosition);

            if (!hasValidatedClickAction && Microsoft.Xna.Framework.Vector2.Distance(clickPosition, pos2d) >= menuActionPixelsTreshold)
            {
                var now = DateTime.Now;

                if ((now - lastRightMenuAction) >= rightMenuActionCooldownDuration)
                {
                    lastRightMenuAction = now;

                    var top = new Microsoft.Xna.Framework.Vector2() { X = 0, Y = 1 };
                    var bottom = new Microsoft.Xna.Framework.Vector2() { X = 0, Y = -1 };
                    var left = new Microsoft.Xna.Framework.Vector2() { X = 1, Y = 0 };
                    var right = new Microsoft.Xna.Framework.Vector2() { X = -1, Y = 0 };

                    var dir = clickPosition - pos2d;

                    var s_top = Microsoft.Xna.Framework.Vector2.Dot(dir, top);
                    var s_bottom = Microsoft.Xna.Framework.Vector2.Dot(dir, bottom);
                    var s_left = Microsoft.Xna.Framework.Vector2.Dot(dir, left);
                    var s_right = Microsoft.Xna.Framework.Vector2.Dot(dir, right);

                    var max_dir = Math.Max(Math.Max(Math.Max(s_top, s_bottom), s_left), s_right);

                    if (max_dir == s_top)
                    {
                        Right_ClickAction_Top();
                    }
                    else if (max_dir == s_bottom)
                    {
                        Right_ClickAction_Bottom();
                    }
                    else if (max_dir == s_left)
                    {
                        Right_ClickAction_Left();
                    }
                    else if (max_dir == s_right)
                    {
                        Right_ClickAction_Right();
                    }
                }
            }
        }

        DateTime lastLeftMenuAction = DateTime.Now;
        TimeSpan leftMenuActionCooldownDuration = TimeSpan.FromMilliseconds(2000);

        private void HandleLeftMenuAction(Microsoft.Xna.Framework.Vector3 currentPosition, Microsoft.Xna.Framework.Vector2 clickPosition)
        {
            var pos2d = XY(currentPosition);

            if (!hasValidatedClickAction && Microsoft.Xna.Framework.Vector2.Distance(clickPosition, pos2d) >= menuActionPixelsTreshold)
            {
                var now = DateTime.Now;

                if ((now - lastLeftMenuAction) >= leftMenuActionCooldownDuration)
                {
                    lastLeftMenuAction = now;

                    var top = new Microsoft.Xna.Framework.Vector2() { X = 0, Y = 1 };
                    var bottom = new Microsoft.Xna.Framework.Vector2() { X = 0, Y = -1 };
                    var left = new Microsoft.Xna.Framework.Vector2() { X = 1, Y = 0 };
                    var right = new Microsoft.Xna.Framework.Vector2() { X = -1, Y = 0 };

                    var dir = clickPosition - pos2d;

                    var s_top = Microsoft.Xna.Framework.Vector2.Dot(dir, top);
                    var s_bottom = Microsoft.Xna.Framework.Vector2.Dot(dir, bottom);
                    var s_left = Microsoft.Xna.Framework.Vector2.Dot(dir, left);
                    var s_right = Microsoft.Xna.Framework.Vector2.Dot(dir, right);

                    var max_dir = Math.Max(Math.Max(Math.Max(s_top, s_bottom), s_left), s_right);

                    hasValidatedClickAction = true;

                    if (max_dir == s_top)
                    {
                        Left_ClickAction_Top();
                    }
                    else if (max_dir == s_bottom)
                    {
                        Left_ClickAction_Bottom();
                    }
                    else if (max_dir == s_left)
                    {
                        Left_ClickAction_Left();
                    }
                    else if (max_dir == s_right)
                    {
                        Left_ClickAction_Right();
                    }
                }
            }
        }

        DateTime lastChangePageActionDate = DateTime.MinValue;

        public void KinectLeftClickBegin(KinectProvider provider, Microsoft.Xna.Framework.Vector3 position)
        {
            var cursorPosition = provider.MainPosition.CurrentPoint.Position;

            /* pour éviter la madness */
            bool canChangePage = (DateTime.Now - lastChangePageActionDate).TotalSeconds >= 2;

            if (canChangePage && cursorPosition.Y < browser.ActualHeight - BrowserMargin && cursorPosition.X <= BrowserMargin)
            {
                browser.TabNext();
                lastChangePageActionDate = DateTime.Now;
            }
            else if (canChangePage && cursorPosition.Y < browser.ActualHeight - BrowserMargin && cursorPosition.X >= browser.ActualWidth - BrowserMargin)
            {
                browser.TabPrev();
                lastChangePageActionDate = DateTime.Now;
            }
            else
            {
                Canvas.SetLeft(additionnalActionsUIControls, position.X);
                Canvas.SetTop(additionnalActionsUIControls, position.Y);

                additionnalActionsUIControls.Visibility = System.Windows.Visibility.Visible;
            }
        }

        #endregion

        #region Statistics

        double lastRenderingDurationMs = -1;
        double lastProcessingTime = -1;

        double renderingAverage = 0;
        double processingAverage = 0;

        void CurrentEngine_AfterRender(object sender, EventArgs e)
        {
            var p_time = (InteractionsManager != null && InteractionsManager.CurrentProvider != null)
                ? (int)InteractionsManager.CurrentProvider.ProcessingTime.TotalMilliseconds
                : (int)0;

            processingAverage = processingAverage * 0.95 + 0.05 * p_time;
            renderingAverage = renderingAverage * 0.95 + 0.05 * SoraEngine.LastRenderingDuration.TotalMilliseconds;

            if (renderingAverage != lastRenderingDurationMs || processingAverage != p_time)
            {
                lastRenderingDurationMs = renderingAverage;
                lastProcessingTime = processingAverage;

                Dispatcher.Invoke((Action)delegate
                {
                    statisticsLabel.Text = String.Format("Rendering time: {0}ms\nProcessing Time: {1}ms",
                        (int)renderingAverage,
                        (int)processingAverage);
                });
            }
        }

        #endregion

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            browser.TabNext();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            //browser.Focus();
            browser.ActivePage.Close();
        }
		
		private void Back_click(object sender, System.Windows.RoutedEventArgs e)
        {
			try
			{
                browser.GoBack();
			}
			catch
			{
				MessageBox.Show("Pas de page précédente", "Erreur");	
			}
		}

		private void Refresh_click(object sender, System.Windows.RoutedEventArgs e)
		{
            browser.Reload();
		}

		private void Home_click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (browser.ActivePage != null)
                browser.ActivePage.Navigate(homepage);
            else
                browser.NewTab(homepage);
		}

		private void Forward_click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
                browser.GoForward();
			}
			catch
			{
				MessageBox.Show("Pas de page suivante", "Erreur");	
			}
		}

		private void GoTo_keyboard(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key.Equals(Key.Return)){
				try
				{
					if (browser.ActivePage != null)
               			browser.ActivePage.Navigate(websiteText.Text);
            		else
                		browser.NewTab(homepage);
				}
				catch
				{
					MessageBox.Show("Mauvaise URL", "Erreur");
				}
			}
		}
		
		private void Bookmark_click(object sender, System.Windows.RoutedEventArgs e)
		{
			Bookmark fav = new Bookmark(browser);
			if (browser.ActivePage != null) 
			{
				fav.urlTxt.Text = browser.ActivePage.CurrentUrl;
				fav.titleTxt.Text = browser.ActivePage.Title;
			}
			fav.ShowDialog();
		}

		private void GoTo_click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (browser.ActivePage != null)
               	browser.ActivePage.Navigate(websiteText.Text);
            else
                browser.NewTab(homepage);
		}

        #region IInputClient Members

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
            var origin = rootGrid.PointToScreen(new Point(0, 0));
            var far = rootGrid.PointToScreen(new Point(rootGrid.ActualWidth, rootGrid.ActualHeight));

            ClientArea = new Microsoft.Xna.Framework.Rectangle((int)origin.X, (int)origin.Y, (int)(far.X - origin.X), (int)(far.Y - origin.Y));
        }

        public Microsoft.Xna.Framework.Rectangle ScreenArea
        {
            get { return WindowUtils.GetScreenArea(this); }
        }

        public Microsoft.Xna.Framework.Rectangle ClientArea { get; private set; }

        #endregion
    }
}
