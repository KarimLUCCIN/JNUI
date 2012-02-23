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

namespace KinectBrowser.Measurements
{
    /// <summary>
    /// Interaction logic for PrecisionMeasurement.xaml
    /// </summary>
    public partial class PrecisionMeasurement : UserControl
    {
        public PrecisionMeasurement()
        {
            InitializeComponent();
        }

        public enum TestMode
        {
            /// <summary>
            /// En attente
            /// </summary>
            Idle,

            /// <summary>
            /// On attend que le curseur arrive sur la zone
            /// </summary>
            WaitForZoneEnter,

            /// <summary>
            /// On attend un certain moment pour voir combien de temps on reste dans la zone
            /// et avec quelle précision
            /// </summary>
            WaitForZoneRest,

            /// <summary>
            /// Test terminé
            /// </summary>
            Finished,
        }

        TestMode mode = TestMode.Idle;

        public TestMode Mode
        {
            get { return mode; }
            set { mode = value;
            LastModeChangeDate = DateTime.Now;
            }
        }

        public DateTime LastModeChangeDate { get; set; }

        int currentTestIndex = 0;
        int maxTestIndex = 10;

        Random rd = new Random(DateTime.Now.Millisecond);

        TimeSpan totalWaitForZoneEnterTime = TimeSpan.Zero;

        double totalDistanceTraveledForEnter = 0;

        double restTotalErrorFromCenter = 0;
        long restTotalStepCount = 0;


        Point testZoneCenter;

        private static double pow2dist(double a, double b)
        {
            var m = a - b;
            return m * m;
        }

        public double DistanceFromTestZoneCenter
        {
            get
            {
                var pos = Mouse.GetPosition(this);
                return Math.Sqrt(pow2dist(pos.X, testZoneCenter.X) + pow2dist(pos.Y, testZoneCenter.Y));
            }
        }

        private static double RadiusFromHeightAndWidth(double a, double b)
        {
            return Math.Sqrt(pow2dist(a, 0) + pow2dist(b, 0));
        }

        TimeSpan restWaitTime = TimeSpan.FromSeconds(2);
        
        public bool Update()
        {
            switch (Mode)
            {
                default:
                case TestMode.Idle:
                    {
                        SelectRandomCursorPosition();
                        Mode = TestMode.WaitForZoneEnter;
                        break;
                    }
                case TestMode.WaitForZoneEnter:
                    {
                        if (DistanceFromTestZoneCenter < RadiusFromHeightAndWidth(32, 32))
                        {
                            cursorTarget.Fill = Brushes.Yellow;
                            totalWaitForZoneEnterTime += DateTime.Now - LastModeChangeDate;
                            Mode = TestMode.WaitForZoneRest;
                        }

                        break;
                    }
                case TestMode.WaitForZoneRest:
                    {
                        restTotalErrorFromCenter += DistanceFromTestZoneCenter;
                        restTotalStepCount++;

                        if (DateTime.Now - LastModeChangeDate >= restWaitTime)
                        {
                            currentTestIndex++;
                            Mode = TestMode.Idle;
                        }
                        break;
                    }
                case TestMode.Finished:
                    break;
            }

            if (currentTestIndex < maxTestIndex)
            {
                return true;
            }
            else
            {
                Mode = TestMode.Finished;
                return false;
            }
        }

        private void SelectRandomCursorPosition()
        {
            var height = ActualHeight;
            var width = ActualWidth;

            do
            {
                testZoneCenter = new Point(rd.NextDouble() * width, rd.NextDouble() * height);
            } while (DistanceFromTestZoneCenter < 100);

            Canvas.SetLeft(cursorTarget, testZoneCenter.X);
            Canvas.SetTop(cursorTarget, testZoneCenter.Y);

            cursorTarget.Fill = Brushes.Gray;
        }

        public void ReportResults()
        {
            if (currentTestIndex > 0)
            {
                MessageBox.Show(String.Format("Temps moyen pour arriver : {0}\nErreur moyenne : {1}",
                    TimeSpan.FromSeconds(totalWaitForZoneEnterTime.TotalSeconds / (double)maxTestIndex),
                    restTotalErrorFromCenter / (double)restTotalStepCount), "Résultats");
            }
        }

        public void Begin()
        {
            currentTestIndex = 0;
            Mode = TestMode.Idle;
        }
    }
}
