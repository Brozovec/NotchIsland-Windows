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
        // Vždy přes UI vlákno – služby mění vlastnosti i z timerů na pozadí (jinak Avalonia vyhodí výjimku a aplikace spadne).
        NowPlayingService.Instance.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(NowPlayingService.IsPlaying) or nameof(NowPlayingService.Artwork) or nameof(NowPlayingService.HasMedia)) OnUi(UpdateCompact); };
        TimerService.Instance.PropertyChanged += (_, _) => OnUi(UpdateCompact);
        BakalariService.Instance.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(BakalariService.WingText) or nameof(BakalariService.IsBreak)) OnUi(UpdateCompact); };
        UpdateCompact();
    }

    static void OnUi(Action a) { if (Dispatcher.UIThread.CheckAccess()) a(); else Dispatcher.UIThread.Post(a); }
    void Place() => SyncWindow();
    double lastW = -1, lastH = -1;
    static double ExpandedHeightFor(string tab) => tab == "timetable" ? 250 : ExpandedH;

    /// Okno má pevnou velikost (max. rozbalený ostrůvek), aby se při animaci nic neposouvalo. Ve sbaleném stavu je pro myš průhledné (click-through),
    /// takže průhledná plocha neblokuje kliknutí pod ní.
    void SyncWindow()
    {
        var scr = Screens.Primary ?? Screens.All[0]; var scale = scr.Scaling;
        double w = ExpandedW + 24, h = 250 + 16;
        if (w != lastW || h != lastH) { lastW = w; lastH = h; Width = w; Height = h; Position = new PixelPoint((int)(scr.Bounds.X + (scr.Bounds.Width - w * scale) / 2), scr.Bounds.Y); }
        Platform.SetClickThrough(TryGetPlatformHandle()?.Handle ?? IntPtr.Zero, !expanded);
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
        expanded = true; Compact.Opacity = 0; Expanded.IsVisible = true; CompactEq.Active = false; DispatcherTimer.RunOnce(() => { if (expanded) Compact.IsVisible = false; }, TimeSpan.FromMilliseconds(150));
        Island.Height = current == "timetable" ? 250 : ExpandedH;
        SyncWindow();
        Island.Width = ExpandedW;
        if (Body.Content == null) Show(current);
        Expanded.Opacity = 1;
    }
    public void Collapse()
    {
        expanded = false; holdOpen = false; Expanded.Opacity = 0;
        Island.Width = CompactWidth(); Island.Height = CollapsedH; SyncWindow();
        DispatcherTimer.RunOnce(() => { if (!expanded) { Expanded.IsVisible = false; Compact.IsVisible = true; Compact.Opacity = 1; CompactEq.Active = CompactEq.IsVisible; } }, TimeSpan.FromMilliseconds(220));
    }

    /// Hover podle skutečné polohy kurzoru (Windows); jinde přes PointerEntered/Exited.
    bool hiddenForFullscreen; DateTime lastFsCheck = DateTime.MinValue; bool fullscreen;
    void Poll()
    {
        var p = Platform.CursorPosition();
        // Fullscreen (video, hra, prezentace): ostrůvek schovat, ukázat jen když kurzor zajede k hornímu okraji.
        if (p != null && !expanded)
        {
            if ((DateTime.Now - lastFsCheck).TotalMilliseconds > 400) { lastFsCheck = DateTime.Now; fullscreen = Platform.IsForegroundFullscreen(); }
            var top = (Screens.Primary ?? Screens.All[0]).Bounds.Y;
            var atTop = p.Value.y <= top + 1;
            if (fullscreen && !atTop && !hiddenForFullscreen) { hiddenForFullscreen = true; Hide(); return; }
            if (hiddenForFullscreen && (!fullscreen || atTop)) { hiddenForFullscreen = false; Show(); }
            if (hiddenForFullscreen) return;
        } if (p == null) { if (!expanded || holdOpen) return; if (outsideSince != DateTime.MaxValue && (DateTime.Now - outsideSince).TotalMilliseconds > 350) { outsideSince = DateTime.MaxValue; Collapse(); } return; }
        var scr = Screens.Primary ?? Screens.All[0]; var s = scr.Scaling;
        var cx = scr.Bounds.X + scr.Bounds.Width / 2.0;
        var w = (expanded ? ExpandedW : CompactWidth()) * s; var h = (expanded ? Island.Height : CollapsedH) * s;
        var inside = p.Value.x >= cx - w / 2 && p.Value.x <= cx + w / 2 && p.Value.y <= scr.Bounds.Y + h + (expanded ? 16 * s : 0);
        if (inside) { outsideSince = DateTime.MaxValue; holdOpen = false; if (!expanded) Expand(); }
        else if (expanded && !holdOpen) { if (outsideSince == DateTime.MaxValue) outsideSince = DateTime.Now; else if ((DateTime.Now - outsideSince).TotalMilliseconds > 350) { outsideSince = DateTime.MaxValue; Collapse(); } }
    }

    // ---- sbalený obsah: hudba / časovač / přestávka
    double CompactWidth() => NowPlayingService.Instance.IsPlaying || TimerService.Instance.IsActive || BakalariService.Instance.WingText != "" ? CollapsedW : 200;
    byte[]? artBytes; Bitmap? artBmp;
    void UpdateCompact()
    {
        var np = NowPlayingService.Instance; var tm = TimerService.Instance; var bk = BakalariService.Instance;
        CompactArtBorder.IsVisible = false; CompactLeftIcon.IsVisible = false; CompactRightText.IsVisible = false; CompactEq.IsVisible = false;
        if (bk.WingText != "") { CompactLeftIcon.Text = bk.IsBreak ? FA.Mug : FA.Book; CompactLeftIcon.FontFamily = FA.Solid; CompactLeftIcon.Foreground = bk.IsBreak ? Brushes.Orange : Brushes.LightGreen; CompactLeftIcon.IsVisible = true; CompactRightText.Text = bk.WingText; CompactRightText.IsVisible = true; }
        else if (tm.IsActive) { CompactLeftIcon.Text = tm.IsPomodoro ? FA.Leaf : FA.Stopwatch; CompactLeftIcon.FontFamily = FA.Solid; CompactLeftIcon.Foreground = tm.IsBreak ? Brushes.DeepSkyBlue : Brushes.Orange; CompactLeftIcon.IsVisible = true; CompactRightText.Text = tm.Text; CompactRightText.IsVisible = true; }
        else if (np.IsPlaying)
        {
            if (np.Artwork != null) { try { if (!ReferenceEquals(np.Artwork, artBytes)) { artBytes = np.Artwork; artBmp = new Bitmap(new MemoryStream(np.Artwork)); } CompactArt.Source = artBmp; CompactArtBorder.IsVisible = true; } catch { } }
            else { CompactLeftIcon.Text = FA.Spotify; CompactLeftIcon.FontFamily = FA.Brands; CompactLeftIcon.Foreground = Brushes.LightGreen; CompactLeftIcon.IsVisible = true; }
            CompactEq.IsVisible = true; CompactEq.Active = true;
        }
        if (!expanded) Island.Width = CompactWidth();
        CompactEq.Active = CompactEq.IsVisible && !expanded;
    }

    public void Toast(string text) => ToastWindow.Show(text, Screens.Primary ?? Screens.All[0]);
    void OpenSettings(object? s, Avalonia.Interactivity.RoutedEventArgs e) { Collapse(); SettingsWindow.ShowIt(); }
    public void RebuildTabs() => BuildTabs();
}
