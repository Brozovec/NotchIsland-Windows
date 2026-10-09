using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace NotchIsland.Views;

/// Malá bublina pod ostrůvkem (vlastní okno, aby neblokovala kliknutí a nevadilo zmenšené hlavní okno).
public class ToastWindow : Window
{
    static ToastWindow? current; static DispatcherTimer? timer;
    public static void Show(string text, Screen scr)
    {
        current?.Close();
        var w = new ToastWindow(text); current = w;
        w.Opened += (_, _) => { var s = scr.Scaling; w.Position = new PixelPoint((int)(scr.Bounds.X + (scr.Bounds.Width - w.Bounds.Width * s) / 2), (int)(scr.Bounds.Y + 52 * s)); };
        w.Show();
        timer?.Stop(); timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
        timer.Tick += (_, _) => { timer!.Stop(); w.Close(); if (current == w) current = null; }; timer.Start();
    }
    ToastWindow(string text)
    {
        SystemDecorations = WindowDecorations.None; Topmost = true; ShowInTaskbar = false; ShowActivated = false; CanResize = false; SizeToContent = SizeToContent.WidthAndHeight;
        Background = Brushes.Transparent; TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Content = new Border { Background = new SolidColorBrush(Color.Parse("#E6000000")), CornerRadius = new CornerRadius(999), Padding = new Thickness(14, 7), Margin = new Thickness(8), Child = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeight.Medium, Foreground = Brushes.White } };
    }
}
