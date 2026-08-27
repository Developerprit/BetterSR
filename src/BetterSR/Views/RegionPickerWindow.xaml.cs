using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Drawing = System.Drawing;

namespace BetterSR.Views;

public partial class RegionPickerWindow : Window
{
    private System.Windows.Point _start;
    private System.Windows.Shapes.Rectangle? _selectionRect;
    private bool _isDragging;

    public Drawing.Rectangle SelectedRegion { get; private set; }

    public RegionPickerWindow()
    {
        InitializeComponent();
    }

    private void OverlayCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _start = e.GetPosition(OverlayCanvas);
        _isDragging = true;

        if (_selectionRect == null)
        {
            _selectionRect = new System.Windows.Shapes.Rectangle
            {
                Fill = new SolidColorBrush(Colors.White) { Opacity = 0.25 },
                Stroke = new SolidColorBrush(Colors.HotPink),
                StrokeThickness = 2
            };
            OverlayCanvas.Children.Add(_selectionRect);
        }

        Canvas.SetLeft(_selectionRect, _start.X);
        Canvas.SetTop(_selectionRect, _start.Y);
        _selectionRect.Width = 0;
        _selectionRect.Height = 0;
    }

    private void OverlayCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging || _selectionRect == null) return;

        var pos = e.GetPosition(OverlayCanvas);
        var x = Math.Min(_start.X, pos.X);
        var y = Math.Min(_start.Y, pos.Y);
        var w = Math.Max(_start.X, pos.X) - x;
        var h = Math.Max(_start.Y, pos.Y) - y;

        Canvas.SetLeft(_selectionRect, x);
        Canvas.SetTop(_selectionRect, y);
        _selectionRect.Width = Math.Max(0, w);
        _selectionRect.Height = Math.Max(0, h);

        InfoText.Text = $"{(int)w} x {(int)h}  ·  Enter 确认 / Esc 取消";
    }

    private void OverlayCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDragging = false;
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (_selectionRect != null && _selectionRect.Width > 0 && _selectionRect.Height > 0)
            {
                SelectedRegion = new Drawing.Rectangle(
                    (int)Canvas.GetLeft(_selectionRect),
                    (int)Canvas.GetTop(_selectionRect),
                    (int)_selectionRect.Width,
                    (int)_selectionRect.Height);
                DialogResult = true;
            }
            Close();
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
        base.OnKeyDown(e);
    }
}
