using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sora.GameEngine.Offscreen;
using System.Runtime.InteropServices;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using Sora.GameEngine.GameComponents.Cameras;
using Microsoft.Xna.Framework;
using Sora.GameEngine.GameComponents.SceneObjects;
using Sora.GameEngine.GameComponents.Animations;
using Sora.GameEngine.GameComponents.Scene;
using Awesomium.Core;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace wpf_3d
{
    public class D3DEffectsScreen : OffscreenEngineVirtualScreen
    {
        Bitmap bmpData;
        int[] bmpByteData;
        Texture2D screenCtrl;

        Node screenContent;

        public Node ScreenContent
        {
            get { return screenContent; }
        }

        SceneObjectTexturedQuad quadFront;
        SceneObjectTexturedQuad quadBack;

        public int CompositionWidth
        {
            get { return bmpData.Width; }
        }

        public int CompositionHeight
        {
            get { return bmpData.Height; }
        }

        public ImageSource D3DImageSource { get; private set; }

        public D3DEffectsScreen(OffscreenEngineInteropBitmap engine)
            : base(engine)
        {
            D3DImageSource = engine.AttachedImage;

            bmpData = new Bitmap(engine.CompositionRT.Width, engine.CompositionRT.Height);
            bmpByteData = new int[bmpData.Width * bmpData.Height];
            screenCtrl = new Texture2D(engine.Device, bmpData.Width, bmpData.Height, false, SurfaceFormat.Color);
        }

        protected override void LoadScreenContent()
        {
            base.LoadScreenContent();

            CameraManager.LoadAndSetActiveCamera(new FixedCamera(CurrentEngine) { NearPlane = 0.1f, FarPlane = 100f, Position = new Vector3(0, 0, -1), Target = new Vector3(0,0,0) });

            screenContent = new Node(LocalContent);
            screenContent.Position = new Vector3(0, 0, (float)Math.Sin(MathHelper.PiOver4) + 0.0225f);

            quadFront = new SceneObjectTexturedQuad(LocalContent);
            quadFront.Texture = screenCtrl;
            quadFront.CompositionTex = CurrentEngine.Renderer.CompositionTexManager.TexColorOnly;

            quadBack = new SceneObjectTexturedQuad(LocalContent);
            quadBack.Texture = screenCtrl;
            quadBack.CompositionTex = CurrentEngine.Renderer.CompositionTexManager.TexColorOnly;
            quadBack.Rotation = new Sora.GameEngine.MathUtils.RotationVector(MathHelper.Pi, 0, 0);

            screenContent.Add(quadFront);
            screenContent.Add(quadBack);

            CurrentEngine.SceneManager.Root.Add(screenContent);
        }

        private const int SRCCOPY = 13369376;

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest,
                                      int nWidth, int nHeight, IntPtr hdcSrc,
                                      int nXSrc, int nYSrc, uint dwRop);

        public void DrawToTexture(System.Windows.Controls.WebBrowser control)
        {
            for (int i = 0; i < 10; i++)
                CurrentEngine.Device.Textures[i] = null;

            DrawToBitmap(control, bmpData);

            unsafe
            {
                // lock bitmap
                System.Drawing.Imaging.BitmapData origdata =
                bmpData.LockBits(new System.Drawing.Rectangle(0, 0, bmpData.Width, bmpData.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpData.PixelFormat);

                uint* byteData = (uint*)origdata.Scan0;

                // Switch bgra -> rgba
                for (int i = 0; i < bmpByteData.Length; i++)
                {
                    byteData[i] = (byteData[i] & 0x000000ff) << 16 | (byteData[i] & 0x0000FF00) | (byteData[i] & 0x00FF0000) >> 16 | (byteData[i] & 0xFF000000);
                }

                // copy data
                System.Runtime.InteropServices.Marshal.Copy(origdata.Scan0, bmpByteData, 0, bmpData.Width * bmpData.Height);

                byteData = null;

                // unlock bitmap
                bmpData.UnlockBits(origdata);
            }

            screenCtrl.SetData<int>(bmpByteData);
        }

        private static void DrawToBitmap(System.Windows.Controls.WebBrowser control, Bitmap bitmap)
        {
            var hdcControl = GetWindowDC(control.Handle);

            if (hdcControl == IntPtr.Zero)
            {
                throw new InvalidOperationException("...");
            }
            else
            {

                try
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        var hdc = graphics.GetHdc();
                        try
                        {
                            BitBlt(hdc, 0, 0,
                               bitmap.Width, bitmap.Height, hdcControl, 0, 0, SRCCOPY);
                        }
                        finally
                        {
                            ReleaseDC(control.Handle, hdcControl);
                        }
                    }
                }
                catch
                {
                    throw new InvalidOperationException("...");
                }
            }
        }


        public void StartAnim()
        {
            var anim = new AnimationFloat(CurrentEngine, TimeSpan.FromSeconds(2),
                new KeyValuePair<float, float>(0, 0),
                new KeyValuePair<float, float>(1, MathHelper.TwoPi));

            anim.Animated += delegate
            {
                Console.WriteLine(anim.Current);
                screenContent.Rotation = new Sora.GameEngine.MathUtils.RotationVector(anim.Current, 0, 0);
            };

            CurrentEngine.AnimationManager.Start(anim);
        }

        internal void UpdateWebTexture(Awesomium.Core.RenderBuffer renderBuffer)
        {
            renderBuffer.RenderTexture2D(screenCtrl);
        }
    }

    public static class AwesomiumXnaExtensions
    {
        public static Texture2D RenderTexture2D(this RenderBuffer Buffer, Texture2D Texture)
        {
            TextureFormatConverter.DirectBlit(Buffer, ref Texture);
            return Texture;
        }
    }
}
