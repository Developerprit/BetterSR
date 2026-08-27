using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace BetterSR.Views;

public partial class WindowPickerWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public IntPtr SelectedHwnd { get; private set; }

    public WindowPickerWindow()
    {
        InitializeComponent();
        LoadWindows();
    }

    private void LoadWindows()
    {
        var list = new List<WindowInfo>();
        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd)) return true;

            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;

            GetWindowRect(hWnd, out var rect);
            var w = rect.Right - rect.Left;
            var h = rect.Bottom - rect.Top;
            if (w <= 0 || h <= 0) return true;

            list.Add(new WindowInfo
            {
                Hwnd = hWnd,
                Title = title,
                SizeText = $"{w} x {h}"
            });
            return true;
        }, IntPtr.Zero);

        WindowList.ItemsSource = list;
        if (list.Count > 0) WindowList.SelectedIndex = 0;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowList.SelectedItem is WindowInfo info)
        {
            SelectedHwnd = info.Hwnd;
            DialogResult = true;
        }
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private class WindowInfo
    {
        public IntPtr Hwnd { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SizeText { get; set; } = string.Empty;
    }
}
