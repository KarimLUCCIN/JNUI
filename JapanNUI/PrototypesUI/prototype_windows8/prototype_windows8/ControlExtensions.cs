using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.Runtime.InteropServices;

namespace prototype_windows8
{
	public static class ControlExtensions
	{        
    	[DllImport("user32.dll")]
    	private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
    	private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    	[DllImport("gdi32.dll")]
    	private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, 
                                      int nWidth, int nHeight, IntPtr hdcSrc, 
                                      int nXSrc, int nYSrc, uint dwRop);

    	private const uint SRCCOPY = 0xCC0020;

    	public static void DrawToBitmap(WebBrowser control, Bitmap bitmap, 
                                    System.Windows.Rect targetBounds)
   		{
        	var width = targetBounds.Width;
        	var height = targetBounds.Height;

        	var hdcControl = GetWindowDC(control.Handle);

        	if (hdcControl == IntPtr.Zero)
        	{
           	 	throw new InvalidOperationException(
                "Could not get a device context for the control.");
        	}

        	try
        	{
            	using (var graphics = Graphics.FromImage(bitmap))
            	{
                	var hdc = graphics.GetHdc();
                	try
                	{
                    	BitBlt(hdc, (int)targetBounds.Left, (int)targetBounds.Top, 
                           (int)width, (int)height, hdcControl, 0, 0, SRCCOPY);
                	}
                	finally
                	{
                    	graphics.ReleaseHdc(hdc);
                	}
            	}
        	}
        	finally
        	{
            	ReleaseDC(control.Handle, hdcControl);
        	}
    	}
	}
}