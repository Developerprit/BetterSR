using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BetterSR.Services;

public class ScreenCaptureService
{
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public Bitmap? CaptureScreen()
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return null;
        return CaptureRegion(screen.Bounds);
    }

    public Bitmap? CaptureRegion(Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return null;

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        try
        {
            using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            return null;
        }
    }

    public Bitmap? CaptureWindow(IntPtr hWnd)
    {
        if (!GetWindowRect(hWnd, out var rect)) return null;
        var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        return CaptureRegion(bounds);
    }

    public Rectangle GetWindowBounds(IntPtr hWnd)
    {
        if (GetWindowRect(hWnd, out var rect))
        {
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }
        return Rectangle.Empty;
    }

    public IntPtr GetForegroundWindowHandle() => GetForegroundWindow();
}
