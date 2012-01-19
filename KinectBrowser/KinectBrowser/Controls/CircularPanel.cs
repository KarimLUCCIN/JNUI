using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace KinectBrowser.Controls
{
    public class CircularPanel : Panel
    {
        public double ChildSize
        {
            get { return (double)GetValue(childWidthProperty); }
            set { SetValue(childWidthProperty, value); }
        }
    
        private static void ChildSizeChangedCallback (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as CircularPanel;

            if(panel != null)
            {
                panel.InvalidateMeasure();
                panel.InvalidateArrange();
            }
        }

        // Using a DependencyProperty as the backing store for childWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty childWidthProperty =
            DependencyProperty.Register("ChildSize", typeof(double), typeof(CircularPanel), new UIPropertyMetadata(10.0, ChildSizeChangedCallback));

        
        // Make the panel as big as the biggest element
        protected override Size MeasureOverride(Size availableSize)
        {
            Size idealSize = new Size(0, 0);

            // Allow children as much room as they want - then scale them

            Size size = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (UIElement child in Children)
            {
                child.Measure(size);
                idealSize.Width = ChildSize;
                idealSize.Height = ChildSize;
            }

            // EID calls us with infinity, but framework

            // doesn't like us to return infinity

            if (double.IsInfinity(availableSize.Height) ||
                double.IsInfinity(availableSize.Width))
                return idealSize;
            else
                return availableSize;
        }

        // Arrange the child elements to their final position
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count > 0)
            {
                var anglePerItem = (Math.PI * 2) / (double)Children.Count;
                var radius = finalSize.Width / 2.0 - ChildSize * 2;

                var center = new Size(finalSize.Width / 2.0, finalSize.Height / 2.0);

                var angle = 0.0;

                for (int i = 0; i < Children.Count; i++)
                {
                    var newCenter = new Point(Math.Cos(angle) * radius + center.Width, Math.Sin(angle) * radius + center.Height);

                    Children[i].Arrange(new Rect(newCenter.X - ChildSize / 2.0, newCenter.Y - ChildSize / 2.0, ChildSize, ChildSize));

                    angle += anglePerItem;
                }
            }

            return finalSize;
        }
    }
}
