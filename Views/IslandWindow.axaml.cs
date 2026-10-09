using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using NotchIsland.Core;
using NotchIsland.Services;

namespace NotchIsland.Views;

/// Ostrůvek nahoře uprostřed obrazovky. Sbalený = pilulka (hudba / časovač / přestávka), najetí = rozbalení s taby.
public partial class IslandWindow : Window
{
    const double CollapsedW = 300, CollapsedH = 36, ExpandedW = 900, ExpandedH = 160, WindowW = 1000, WindowH = 300;
    public record Tab(string Id, string Icon, string Title, Func<Control> Make);
    readonly List<Tab> tabs = new();
    readonly Dictionary<string, Control> views = new();
    string current = "home";
    bool expanded; bool holdOpen; DateTime outsideSince = DateTime.MaxValue;
    readonly DispatcherTimer poll = new() { Interval = TimeSpan.FromMilliseconds(33) };

    public IslandWindow()
    {
        InitializeComponent();
        tabs.Add(new("home", FA.House, L.T("Island"), () => new HomeView()));
        tabs.Add(new("tray", FA.Inbox, L.T("Tray"), () => new TrayView()));
        tabs.Add(new("transit", FA.Train, L.T("Transit"), () => new TransitView()));
        tabs.Add(new("calls", FA.Phone, L.T("Calls"), () => new CallsView()));
        tabs.Add(new("shot", FA.Camera, L.T("Shot"), () => new ShotView()));
        tabs.Add(new("notes", FA.Note, L.T("Notes"), () => new NotesView()));
        tabs.Add(new("clipboard", FA.Clipboard, L.T("Clipboard"), () => new ClipboardView()));
        tabs.Add(new("timer", FA.Stopwatch, L.T("Timer"), () => new TimerView()));
        tabs.Add(new("timetable", FA.Graduation, L.T("Timetable"), () => new TimetableView()));
        BuildTabs();
        Island.Width = CollapsedW; Island.Height = CollapsedH;
        Opened += (_, _) => { Place(); poll.Start(); };
        poll.Tick += (_, _) => Poll();
        PointerEntered += (_, _) => { if (!expanded) Expand(); holdOpen = false; outsideSince = DateTime.MaxValue; };
        PointerExited += (_, _) => { if (Platform.CursorPosition() == null) outsideSince = DateTime.Now; };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Collapse(); };
        // schránka ↔ UI
        ClipboardService.Instance.ReadClipboard = async () => { try { return await Clipboard!.TryGetTextAsync(); } catch { return null; } };
        ClipboardService.Instance.WriteClipboard = async s => { try { await Clipboard!.SetTextAsync(s); } catch { } };
        ClipboardService.Instance.PasteRequested = () => { Collapse(); DispatcherTimer.RunOnce(Platform.SendPaste, TimeSpan.FromMilliseconds(250)); };
        ShotService.Instance.CopyText = async s => { try { await Clipboard!.SetTextAsync(s); } catch { } };
        ShotService.Instance.CopyImage = async png => { try { await Clipboard!.SetBitmapAsync(new Bitmap(new MemoryStream(png))); } catch { } };
        ShotService.Instance.SelectArea = async () => { Collapse(); await Task.Delay(200); return await AreaSelectWindow.PickAsync(); };
        // sbalený obsah
        NowPlayingService.Instance.PropertyChanged += (_, _) => UpdateCompact();
        TimerService.Instance.PropertyChanged += (_, _) => UpdateCompact();
        BakalariService.Instance.PropertyChanged += (_, _) => UpdateCompact();
        UpdateCompact();
    }

    void Place() => SyncWindow();
    static double ExpandedHeightFor(string tab) => tab == "timetable" ? 250 : ExpandedH;

    /// Okno má vždy přesně velikost ostrůvku (+ malá rezerva na stín), jinak by průhledná plocha blokovala kliknutí pod ní.
    void SyncWindow()
    {
        var scr = Screens.Primary ?? Screens.All[0]; var scale = scr.Scaling;
        double w = (expanded ? ExpandedW : CompactWidth()) + 24, h = (expanded ? ExpandedHeightFor(current) : CollapsedH) + 16;
        Width = w; Height = h;
        Position = new PixelPoint((int)(scr.Bounds.X + (scr.Bounds.Width - w * scale) / 2), scr.Bounds.Y);
    }

    void BuildTabs()
    {
        Tabs.Children.Clear();
        foreach (var t in tabs.Where(t => t.Id == "home" || !Settings.Current.DisabledTabs.Contains(t.Id)))
        {
            var b = new Button { Classes = { "pill" }, Content = t.Id == current ? FA.Label(t.Icon, t.Title, 11) : FA.Icon(t.Icon, 11, false, new SolidColorBrush(Color.Parse("#8A8A96"))), Tag = t.Id };
            if (t.Id == current) b.Classes.Add("on");
            b.Click += (_, _) => Show(t.Id);
            Tabs.Children.Add(b);
        }
    }

    public void Show(string id)
    {
        current = id; BuildTabs();
        if (!views.TryGetValue(id, out var v)) { v = tabs.First(t => t.Id == id).Make(); views[id] = v; }
        Body.Content = v;
        Island.Height = id == "timetable" ? 250 : ExpandedH;
        if (expanded) SyncWindow();
    }

    public void Open(string tab) { try { Show(tab); if (!expanded) Expand(); holdOpen = true; Activate(); } catch (Exception e) { Log.W("Open failed: " + e); } }

    void Expand()
    {
        expanded = true; Compact.IsVisible = false; Expanded.IsVisible = true;
        Island.Height = current == "timetable" ? 250 : ExpandedH;
        SyncWindow();   // nejdřív zvětšit okno, pak animovat obsah
        Island.Width = ExpandedW;
        if (Body.Content == null) Show(current);
        Expanded.Opacity = 1;
    }
    public void Collapse()
    {
        expanded = false; holdOpen = false; Expanded.Opacity = 0;
        Island.Width = CompactWidth(); Island.Height = CollapsedH;
        DispatcherTimer.RunOnce(() => { if (!expanded) { Expanded.IsVisible = false; Compact.IsVisible = true; } }, TimeSpan.FromMilliseconds(200));
    }

    /// Hover podle skutečné polohy kurzoru (Windows); jinde přes PointerEntered/Exited.
    void Poll()
    {
        var p = Platform.CursorPosition(); if (p == null) { if (!expanded || holdOpen) return; if (outsideSince != DateTime.MaxValue && (DateTime.Now - outsideSince).TotalMilliseconds > 350) { outsideSince = DateTime.MaxValue; Collapse(); } return; }
        var scr = Screens.Primary ?? Screens.All[0]; var s = scr.Scaling;
        var cx = scr.Bounds.X + scr.Bounds.Width / 2.0;
        var w = (expanded ? ExpandedW : CompactWidth()) * s; var h = (expanded ? Island.Height : CollapsedH) * s;
        var inside = p.Value.x >= cx - w / 2 && p.Value.x <= cx + w / 2 && p.Value.y <= scr.Bounds.Y + h + (expanded ? 16 * s : 0);
        if (inside) { outsideSince = DateTime.MaxValue; holdOpen = false; if (!expanded) Expand(); }
        else if (expanded && !holdOpen) { if (outsideSince == DateTime.MaxValue) outsideSince = DateTime.Now; else if ((DateTime.Now - outsideSince).TotalMilliseconds > 350) { outsideSince = DateTime.MaxValue; Collapse(); } }
    }

    // ---- sbalený obsah: hudba / časovač / přestávka
    double CompactWidth() => NowPlayingService.Instance.IsPlaying || TimerService.Instance.IsActive || BakalariService.Instance.WingText != "" ? CollapsedW : 200;
    void UpdateCompact()
    {
        var np = NowPlayingService.Instance; var tm = TimerService.Instance; var bk = BakalariService.Instance;
        CompactArtBorder.IsVisible = false; CompactLeftIcon.IsVisible = false; CompactRightText.IsVisible = false; CompactEq.IsVisible = false;
        if (bk.WingText != "") { CompactLeftIcon.Text = bk.IsBreak ? FA.Mug : FA.Book; CompactLeftIcon.FontFamily = FA.Solid; CompactLeftIcon.Foreground = bk.IsBreak ? Brushes.Orange : Brushes.LightGreen; CompactLeftIcon.IsVisible = true; CompactRightText.Text = bk.WingText; CompactRightText.IsVisible = true; }
        else if (tm.IsActive) { CompactLeftIcon.Text = tm.IsPomodoro ? FA.Leaf : FA.Stopwatch; CompactLeftIcon.FontFamily = FA.Solid; CompactLeftIcon.Foreground = tm.IsBreak ? Brushes.DeepSkyBlue : Brushes.Orange; CompactLeftIcon.IsVisible = true; CompactRightText.Text = tm.Text; CompactRightText.IsVisible = true; }
        else if (np.IsPlaying)
        {
            if (np.Artwork != null) { try { CompactArt.Source = new Bitmap(new MemoryStream(np.Artwork)); CompactArtBorder.IsVisible = true; } catch { } }
            else { CompactLeftIcon.Text = FA.Spotify; CompactLeftIcon.FontFamily = FA.Brands; CompactLeftIcon.Foreground = Brushes.LightGreen; CompactLeftIcon.IsVisible = true; }
            CompactEq.IsVisible = true; CompactEq.Active = true;
        }
        if (!expanded) { Island.Width = CompactWidth(); SyncWindow(); }
    }

    public void Toast(string text) => ToastWindow.Show(text, Screens.Primary ?? Screens.All[0]);
    void OpenSettings(object? s, Avalonia.Interactivity.RoutedEventArgs e) { Collapse(); SettingsWindow.ShowIt(); }
    public void RebuildTabs() => BuildTabs();
}
