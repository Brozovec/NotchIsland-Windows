using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace NotchIsland.Views;

/// Animovaný equalizer (vizualizace, ne analýza zvuku).
public class Equalizer : Control
{
    public static readonly StyledProperty<bool> ActiveProperty = AvaloniaProperty.Register<Equalizer, bool>(nameof(Active), true);
    public bool Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    public IBrush Fill { get; set; } = new SolidColorBrush(Color.Parse("#3FE36F"));
    readonly double[] levels = { 0.3, 0.6, 0.4, 0.8 };
    readonly Random rnd = new();
    readonly DispatcherTimer t = new() { Interval = TimeSpan.FromMilliseconds(120) };
    public Equalizer() { t.Tick += (_, _) => { if (!Active) return; for (int i = 0; i < levels.Length; i++) levels[i] = 0.15 + rnd.NextDouble() * 0.85; InvalidateVisual(); }; t.Start(); }
    public override void Render(DrawingContext ctx)
    {
        var n = levels.Length; var gap = 3.0; var bw = (Bounds.Width - gap * (n - 1)) / n;
        for (int i = 0; i < n; i++) { var h = Math.Max(2, Bounds.Height * (Active ? levels[i] : 0.12)); ctx.DrawRectangle(Fill, null, new RoundedRect(new Rect(i * (bw + gap), Bounds.Height - h, bw, h), bw / 2)); }
    }
}
