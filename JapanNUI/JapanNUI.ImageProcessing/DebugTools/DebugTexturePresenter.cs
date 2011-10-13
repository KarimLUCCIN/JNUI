using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Microsoft.Xna.Framework.Graphics;

namespace JapanNUI.ImageProcessing.DebugTools
{
    public class DebugTexturePresenter
    {
        internal Window presentWindow;
        internal Image presentImage;

        public int Width{get;private set;}
        public int Height{get;private set;}

        public DebugTexturePresenter(string title, int width, int height)
        {
            Width = width;
            Height = height;

            presentWindow = new Window();
            presentWindow.Title = title;
            presentWindow.WindowStyle = WindowStyle.SingleBorderWindow;
            
            presentWindow.Width = width;
            presentWindow.Height = height;
            presentWindow.Title = title;

            presentImage = new Image();
            
            presentWindow.Content = presentImage;

            presentWindow.Show();
        }

        public void Update(byte[] frame, PixelFormat format, int stride)
        {
            presentWindow.Dispatcher.Invoke((Action)delegate
            {
                presentImage.Source = BitmapSource.Create(
                    Width, Height, 96, 96, format, null, frame, Width * stride);
            });
        }

        private byte[] dataCopyBuffer;

        public void Update(RenderTarget2D texture, PixelFormat format, int stride)
        {
            /* extract data from texture */
            if (dataCopyBuffer == null)
            {
                dataCopyBuffer = new byte[stride * Width * Height];
            }

            texture.GetData<byte>(dataCopyBuffer);

            Update(dataCopyBuffer, format, stride);
        }
    }
}
