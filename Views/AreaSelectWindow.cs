using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using NotchIsland.Services;

namespace NotchIsland.Views;

/// Celoobrazovkový overlay pro výběr oblasti screenshotu (tažením). Esc = zrušit.
public class AreaSelectWindow : Window
{
    readonly TaskCompletionSource<(int, int, int, int)?> tcs = new();
    Point? start; Rect sel;
    public static Task<(int x, int y, int w, int h)?> PickAsync() { var w = new AreaSelectWindow(); w.Show(); w.Activate(); return w.tcs.Task; }
    AreaSelectWindow()
    {
        SystemDecorations = WindowDecorations.None; Topmost = true; ShowInTaskbar = false; Background = new SolidColorBrush(Color.Parse("#40000000"));
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent }; Cursor = new Cursor(StandardCursorType.Cross);
        var (sw, sh) = Platform.ScreenSize(); Position = new PixelPoint(0, 0); Width = sw; Height = sh; WindowState = WindowState.FullScreen;
        PointerPressed += (_, e) => { start = e.GetPosition(this); };
        PointerMoved += (_, e) => { if (start is { } s) { var p = e.GetPosition(this); sel = new Rect(Math.Min(s.X, p.X), Math.Min(s.Y, p.Y), Math.Abs(p.X - s.X), Math.Abs(p.Y - s.Y)); InvalidateVisual(); } };
        PointerReleased += (_, _) => { var sc = Screens.Primary?.Scaling ?? 1; Close(); tcs.TrySetResult(sel.Width > 2 && sel.Height > 2 ? ((int)(sel.X * sc), (int)(sel.Y * sc), (int)(sel.Width * sc), (int)(sel.Height * sc)) : null); };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); tcs.TrySetResult(null); } };
    }
    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        if (sel.Width > 0) { ctx.DrawRectangle(new SolidColorBrush(Color.Parse("#20FFFFFF")), new Pen(Brushes.White, 1), sel); }
    }
}
