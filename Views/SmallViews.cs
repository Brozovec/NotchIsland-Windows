using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NotchIsland.Core;
using NotchIsland.Services;

namespace NotchIsland.Views;

public class CallsView : UserControl
{
    public CallsView()
    {
        var c = CallsService.Instance;
        var title = new TextBlock { FontSize = 12, FontWeight = FontWeight.SemiBold }; var chips = new WrapPanel { Orientation = Orientation.Horizontal };
        void R() { title.Text = c.Running.Count > 0 ? "☎ " + L.T("On a call") + "?" : "☎ " + L.T("No call"); chips.Children.Clear(); if (c.Running.Count == 0) chips.Children.Add(new TextBlock { Text = L.T("No call app running"), FontSize = 10, Classes = { "muted" } }); foreach (var a in c.Running) chips.Children.Add(new Border { Background = new SolidColorBrush(Color.Parse(a.Color)), CornerRadius = new CornerRadius(999), Padding = new Thickness(8, 3), Margin = new Thickness(0, 0, 4, 4), Child = new TextBlock { Text = a.Name, FontSize = 10, FontWeight = FontWeight.Medium } }); }
        c.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(R); R();
        Content = new Border { Classes = { "tile" }, Child = new StackPanel { Spacing = 8, Children = { title, chips } } };
    }
}

public class ShotView : UserControl
{
    public ShotView()
    {
        var s = ShotService.Instance;
        Button B(string label, string key, string mode) { var b = new Button { Classes = { "chip" }, Content = $"{label}   {key}", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 4) }; b.Click += async (_, _) => await s.CaptureAsync(mode); return b; }
        var buttons = new StackPanel { Width = 170, Children = { B("⬚ " + L.T("Area"), "Ctrl⇧2", "area"), B("▭ " + L.T("Screen"), "Ctrl⇧1", "screen"), B("𝐓 " + L.T("Text (OCR)"), "Ctrl⇧O", "ocr") } };
        var strip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var empty = new TextBlock { Text = L.T("No screenshots yet"), Classes = { "muted" }, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        void R() { strip.Children.Clear(); empty.IsVisible = s.Shots.Count == 0; foreach (var sh in s.Shots) { var img = new Image { Stretch = Stretch.UniformToFill, Width = 128, Height = 84 }; try { img.Source = new Bitmap(new MemoryStream(sh.Png)); } catch { } var b = new Border { CornerRadius = new CornerRadius(8), ClipToBounds = true, Child = img, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) }; b.DoubleTapped += (_, _) => Platform.Reveal(sh.Path); b.PointerPressed += async (_, _) => { if (s.CopyImage != null) await s.CopyImage(sh.Png); App.Island?.Toast(L.T("Copied to clipboard")); }; strip.Children.Add(b); } }
        s.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(R); R();
        Content = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*"), Children = { buttons, Col(new Grid { Children = { empty, new ScrollViewer { Content = strip, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto } } }, 2) } };
    }
    static Control Col(Control c, int i) { Grid.SetColumn(c, i); return c; }
}

public class NotesView : UserControl
{
    public NotesView()
    {
        var n = NotesService.Instance;
        var box = new TextBox { Classes = { "dark" }, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Watermark = L.T("Quick note… saves automatically"), Text = n.Text, VerticalContentAlignment = VerticalAlignment.Top, FontSize = 12 };
        box.TextChanged += (_, _) => n.Text = box.Text ?? "";
        var copy = new Button { Classes = { "icon" }, Content = "⧉" }; copy.Click += async (_, _) => { try { await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(n.Text); App.Island?.Toast(L.T("Copied to clipboard")); } catch { } };
        var clear = new Button { Classes = { "icon" }, Content = "🗑" }; clear.Click += (_, _) => { box.Text = ""; };
        Content = new Grid { Children = { box, new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(6), Children = { copy, clear } } } };
    }
}

public class ClipboardView : UserControl
{
    public ClipboardView()
    {
        var c = ClipboardService.Instance;
        var search = new TextBox { Classes = { "dark" }, Watermark = L.T("Search clipboard…") }; search.TextChanged += (_, _) => c.Query = search.Text ?? "";
        var clear = new Button { Classes = { "icon" }, Content = "🗑" }; clear.Click += (_, _) => c.Clear();
        var list = new StackPanel(); var empty = new TextBlock { Text = L.T("Anything you copy shows up here. Click = paste."), Classes = { "muted" }, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        void R()
        {
            list.Children.Clear(); var items = c.Filtered.ToList(); empty.IsVisible = items.Count == 0;
            foreach (var it in items)
            {
                var txt = new TextBlock { Text = it.Text.Replace("\n", " ⏎ "), FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
                var age = new TextBlock { Text = Ago(it.Date), FontSize = 9, Classes = { "dim" }, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
                var pin = new Button { Classes = { "icon" }, Content = it.Pinned ? "📌" : "📍", FontSize = 10, Opacity = it.Pinned ? 1 : 0.5 }; pin.Click += (_, e) => { e.Handled = true; c.TogglePin(it); };
                var del = new Button { Classes = { "icon" }, Content = "×", FontSize = 10 }; del.Click += (_, e) => { e.Handled = true; c.Remove(it); };
                var row = new Border { Classes = { "row" }, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand), Child = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto"), Children = { txt, Col(age, 1), Col(pin, 2), Col(del, 3) } } };
                row.PointerPressed += async (_, _) => await c.PasteAsync(it);
                list.Children.Add(row);
            }
        }
        c.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(R); R();
        Content = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Children = { new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), Children = { search, Col(new TextBlock { Text = "Ctrl⇧V", FontSize = 9, Classes = { "dim" }, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) }, 1), Col(clear, 2) } }, Row(new Grid { Margin = new Thickness(0, 4, 0, 0), Children = { empty, new ScrollViewer { Content = list } } }, 1) } };
    }
    static string Ago(DateTime d) { var s = DateTime.Now - d; return s.TotalMinutes < 1 ? $"{(int)s.TotalSeconds} s" : s.TotalHours < 1 ? $"{(int)s.TotalMinutes} min" : s.TotalDays < 1 ? $"{(int)s.TotalHours} h" : $"{(int)s.TotalDays} d"; }
    static Control Col(Control c, int i) { Grid.SetColumn(c, i); return c; }
    static Control Row(Control c, int i) { Grid.SetRow(c, i); return c; }
}

public class TimerView : UserControl
{
    public TimerView()
    {
        var t = TimerService.Instance;
        var ring = new Avalonia.Controls.Shapes.Arc { Width = 84, Height = 84, StrokeThickness = 6, Stroke = new SolidColorBrush(Color.Parse("#FF9F0A")), StartAngle = -90, SweepAngle = 0 };
        var track = new Avalonia.Controls.Shapes.Ellipse { Width = 84, Height = 84, StrokeThickness = 6, Stroke = new SolidColorBrush(Color.Parse("#1FFFFFFF")) };
        var txt = new TextBlock { FontSize = 18, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var sub = new TextBlock { FontSize = 8, Classes = { "muted" }, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 46, 0, 0) };
        var circle = new Grid { Width = 84, Height = 84, Children = { track, ring, txt, sub }, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
        circle.PointerPressed += (_, _) => { if (t.IsActive) t.Toggle(); };
        var presets = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var m in new[] { 5, 10, 15, 25, 45, 60 }) { var b = new Button { Classes = { "chip" }, Content = m.ToString(), Margin = new Thickness(0, 0, 6, 6) }; b.Click += (_, _) => t.Start(m); presets.Children.Add(b); }
        var pomo = new Button { Classes = { "chip" }, Content = "🍅 Pomodoro", Background = new SolidColorBrush(Color.Parse("#59FF9F0A")), Margin = new Thickness(0, 0, 6, 6) }; pomo.Click += (_, _) => t.Start(25, true); presets.Children.Add(pomo);
        var custom = new TextBox { Classes = { "dark" }, Watermark = L.T("min"), Width = 56 }; custom.KeyDown += (_, e) => { if (e.Key == Avalonia.Input.Key.Enter && int.TryParse(custom.Text, out var m) && m > 0) { t.Start(m); custom.Text = ""; } };
        var pause = new Button { Classes = { "chip" }, Content = "⏸" }; pause.Click += (_, _) => t.Toggle();
        var plus = new Button { Classes = { "chip" }, Content = "+1" }; plus.Click += (_, _) => t.AddMinute();
        var stop = new Button { Classes = { "chip" }, Content = "■" }; stop.Click += (_, _) => t.Stop();
        var ctl = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { custom, pause, plus, stop } };
        void R() { txt.Text = t.IsActive ? t.Text : "0:00"; ring.SweepAngle = t.Total > 0 ? 360 * t.Remaining / t.Total : 0; ring.Stroke = new SolidColorBrush(Color.Parse(t.IsBreak ? "#5AC8FA" : "#FF9F0A")); sub.Text = t.IsPomodoro ? (t.IsBreak ? L.T("break") : $"{L.T("work")} {t.Round}") : ""; pause.Content = t.Running ? "⏸" : "▶"; pause.IsVisible = plus.IsVisible = stop.IsVisible = t.IsActive; }
        t.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(R); R();
        Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { circle, new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center, Children = { presets, ctl } } } };
    }
}
