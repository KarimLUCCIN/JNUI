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
using System.IO;
using System.Reflection;

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

        /// <summary>
        /// Le temps passé en attente du fait de rentrer dans la zone
        /// </summary>
        TimeSpan totalWaitForZoneEnterTime = TimeSpan.Zero;

        /// <summary>
        /// Le temps passé à l'intérieur de la zone en mode rest
        /// </summary>
        TimeSpan totalTimePassedInsideTheZone = TimeSpan.Zero;

        /// <summary>
        /// Le temps total passé en mode rest
        /// </summary>
        TimeSpan totalZoneTestTime = TimeSpan.Zero;

        /// <summary>
        /// La distance totale parcourue en mode rest à partir du moment où on rentre dans la zone
        /// </summary>
        double totalZoneStabilityDistance = 0;

        double totalDistanceTraveledForEnter = 0;

        double restTotalErrorFromCenter = 0;
        long restTotalStepCount = 0;


        Point testZoneCenter;

        private static double pow2dist(double a, double b)
        {
            var m = a - b;
            return m * m;
        }

        private static double dist(ref Point a, ref Point b)
        {
            return Math.Sqrt(pow2dist(a.X, b.X) + pow2dist(a.Y, b.Y));
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

        DateTime lastUpdateTime = DateTime.Now;

        Point lastMousePoint = new Point(0, 0);

        public double ZoneSize
        {
            get { return cursorTarget.Width / 2.0; }
        }
        
        public bool Update()
        {
            var mousePos = Mouse.GetPosition(this);

            if(lastUpdateTime > LastModeChangeDate)
                lastUpdateTime = LastModeChangeDate;

            var elapsed = DateTime.Now - lastUpdateTime;

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
                        if (DistanceFromTestZoneCenter < RadiusFromHeightAndWidth(ZoneSize, ZoneSize))
                        {
                            cursorTarget.Fill = Brushes.Yellow;
                            totalWaitForZoneEnterTime += DateTime.Now - LastModeChangeDate;
                            Mode = TestMode.WaitForZoneRest;
                        }

                        break;
                    }
                case TestMode.WaitForZoneRest:
                    {
                        if (DistanceFromTestZoneCenter < RadiusFromHeightAndWidth(ZoneSize, ZoneSize))
                            totalTimePassedInsideTheZone += elapsed;

                        totalZoneTestTime += elapsed;

                        restTotalErrorFromCenter += DistanceFromTestZoneCenter;
                        restTotalStepCount++;

                        totalZoneStabilityDistance += dist(ref mousePos, ref lastMousePoint);

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

            lastUpdateTime = DateTime.Now;
            lastMousePoint = mousePos;

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
                string resultsString = String.Format("Temps moyen pour arriver : {0}\nErreur moyenne : {1}\nPourcentage du temps passé dans la zone : {2}\nDistance parcourue en rest (stabilité) : {3}",
                    TimeSpan.FromSeconds(totalWaitForZoneEnterTime.TotalSeconds / (double)maxTestIndex),
                    restTotalErrorFromCenter / (double)restTotalStepCount,
                    100 * (totalTimePassedInsideTheZone.TotalSeconds / totalZoneTestTime.TotalSeconds),
                    totalZoneStabilityDistance);

                using (var stream = new FileStream(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\mesures.txt", FileMode.Append))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine(String.Format("Mode : {0}", KinectBrowser.Input.Kinect.KinectProvider.HasKinects ? "Kinect" : "Mouse"));
                        writer.WriteLine(String.Format("Zone Size : {0}", ZoneSize));
                        writer.WriteLine(resultsString);
                        writer.WriteLine("\n------------------------\n\n");
                    }
                }

                MessageBox.Show(resultsString, "Résultats");
            }
        }

        public void Begin()
        {
            currentTestIndex = 0;
            Mode = TestMode.Idle;
        }
    }
}
