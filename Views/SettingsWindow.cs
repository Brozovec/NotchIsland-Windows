using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using NotchIsland.Core;
using NotchIsland.Services;

namespace NotchIsland.Views;

/// Nastavení: vlastní tmavé okno s postranní navigací a kartami (stejný jazyk jako ostrůvek, ne výchozí Windows vzhled).
public class SettingsWindow : Window
{
    static SettingsWindow? inst;
    public static void ShowIt() { if (inst == null) { inst = new SettingsWindow(); inst.Closed += (_, _) => inst = null; } inst.Show(); inst.Activate(); }

    static readonly IBrush Bg = new SolidColorBrush(Color.Parse("#161619")), Side = new SolidColorBrush(Color.Parse("#1C1C21"));
    readonly StackPanel nav = new() { Spacing = 2 };
    readonly ContentControl page = new();
    readonly Dictionary<string, Func<Control>> pages = new();

    SettingsWindow()
    {
        Title = L.T("Settings") + " – NotchIsland"; Width = 780; Height = 540; MinWidth = 640; MinHeight = 420;
        SystemDecorations = WindowDecorations.None; Background = Brushes.Transparent; TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        CanResize = true; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        try { Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://NotchIsland/Assets/icon.png"))); } catch { }
        var s = Settings.Current;

        pages["general"] = () => General(s);
        pages["modules"] = () => Modules(s);
        pages["calendar"] = () => Calendar(s);
        pages["weather"] = () => Weather(s);
        pages["transit"] = () => Transit(s);
        pages["bakalari"] = () => Bakalari(s);
        pages["shot"] = () => Shot(s);

        // levý panel
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(14, 16, 14, 14) };
        try { brand.Children.Add(new Image { Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(new Uri("avares://NotchIsland/Assets/icon.png"))), Width = 26, Height = 26 }); } catch { }
        brand.Children.Add(new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { new TextBlock { Text = "NotchIsland", FontSize = 13, FontWeight = FontWeight.Bold }, new TextBlock { Text = L.T("Settings"), FontSize = 10, Classes = { "muted" } } } });
        foreach (var (id, icon, title) in new[] { ("general", FA.Sliders, L.T("General")), ("modules", FA.Grid, L.T("Modules")), ("calendar", FA.Calendar, L.T("Calendar")), ("weather", FA.CloudSun, L.T("Weather")), ("transit", FA.Train, L.T("Transit")), ("bakalari", FA.Graduation, "Bakaláři"), ("shot", FA.Camera, L.T("Shot")) })
        {
            var b = new Button { Classes = { "nav" }, Tag = id, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Content = FA.Label(icon, title, 12) };
            b.Click += (_, _) => Open(id); nav.Children.Add(b);
        }
        var version = new TextBlock { Text = "v1.1 · Adam Brož", FontSize = 10, Classes = { "dim" }, Margin = new Thickness(14, 0, 14, 14), VerticalAlignment = VerticalAlignment.Bottom };
        var side = new Border { Background = Side, CornerRadius = new CornerRadius(14, 0, 0, 14), Width = 190, Child = new DockPanel { Children = { Dock(brand, Avalonia.Controls.Dock.Top), Dock(version, Avalonia.Controls.Dock.Bottom), new Border { Padding = new Thickness(8, 0), Child = nav } } } };

        // záhlaví (táhnutí + zavřít)
        var close = new Button { Classes = { "icon" }, Content = FA.Icon(FA.Xmark, 12, false, new SolidColorBrush(Color.Parse("#9A9AA6"))), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 10, 0), Width = 28, Height = 28, CornerRadius = new CornerRadius(8) };
        close.Click += (_, _) => Close();
        var titleBar = new Border { Height = 40, Background = Brushes.Transparent, Child = close };
        titleBar.PointerPressed += (_, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e); };
        side.PointerPressed += (_, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is not Button) BeginMoveDrag(e); };

        var right = new DockPanel { Children = { Dock(titleBar, Avalonia.Controls.Dock.Top), new ScrollViewer { Content = page, Padding = new Thickness(26, 0, 26, 22) } } };
        Content = new Border { Background = Bg, CornerRadius = new CornerRadius(14), BorderBrush = new SolidColorBrush(Color.Parse("#2A2A30")), BorderThickness = new Thickness(1), Child = new Grid { ColumnDefinitions = new ColumnDefinitions("190,*"), Children = { side, Col(right, 1) } } };
        Open("general");
    }

    void Open(string id)
    {
        foreach (var c in nav.Children.OfType<Button>()) { if ((string)c.Tag! == id) c.Classes.Add("on"); else c.Classes.Remove("on"); }
        page.Content = pages[id]();
    }

    // ---------- stránky
    Control General(Settings s)
    {
        var login = new ToggleSwitch { IsChecked = s.LaunchAtLogin, OnContent = "", OffContent = "" };
        login.IsCheckedChanged += (_, _) => { s.LaunchAtLogin = login.IsChecked == true; s.Save(); Platform.SetLaunchAtLogin(s.LaunchAtLogin); };
        var lang = new ComboBox { ItemsSource = new[] { L.T("System"), "English", "Čeština" }, SelectedIndex = s.Language == "" ? 0 : s.Language == "cs" ? 2 : 1, Width = 150 };
        lang.SelectionChanged += (_, _) => { s.Language = lang.SelectedIndex switch { 1 => "en", 2 => "cs", _ => "" }; s.Save(); };
        return Page(L.T("General"), L.T("Startup and language"),
            Card(Row(L.T("Launch at login"), L.T("NotchIsland starts quietly with Windows and lives at the top edge of the screen."), login),
                 Row(L.T("Language"), L.T("Takes effect after restart."), lang)),
            Card(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { FA.Icon(FA.Info, 14, false, new SolidColorBrush(Color.Parse("#8A8A96"))), new TextBlock { Text = "NotchIsland for Windows 1.1 · Adam Brož · notchisland.brozovec.eu", FontSize = 11, Classes = { "muted" }, VerticalAlignment = VerticalAlignment.Center } } }));
    }

    Control Modules(Settings s)
    {
        var list = new StackPanel { Spacing = 2 };
        foreach (var (id, icon, name, desc) in new[] { ("tray", FA.Inbox, L.T("Tray"), L.T("Drop files to keep them at hand")), ("transit", FA.Train, L.T("Transit"), L.T("Departures, RegioJet and FlixBus")), ("calls", FA.Phone, L.T("Calls"), L.T("Discord, Teams, Zoom status")), ("shot", FA.Camera, L.T("Shot"), L.T("Screenshots and text recognition")), ("notes", FA.Note, L.T("Notes"), L.T("Quick note that saves itself")), ("clipboard", FA.Clipboard, L.T("Clipboard"), L.T("History of copied text")), ("timer", FA.Stopwatch, L.T("Timer"), L.T("Timer and Pomodoro")), ("timetable", FA.Graduation, L.T("Timetable"), L.T("Bakaláři school timetable")) })
        {
            var sw = new ToggleSwitch { IsChecked = !s.DisabledTabs.Contains(id), OnContent = "", OffContent = "" };
            sw.IsCheckedChanged += (_, _) => { if (sw.IsChecked == true) s.DisabledTabs.Remove(id); else if (!s.DisabledTabs.Contains(id)) s.DisabledTabs.Add(id); s.Save(); App.Island?.RebuildTabs(); };
            list.Children.Add(Row(name, desc, sw, icon));
        }
        return Page(L.T("Modules"), L.T("Choose which tabs the island shows"), Card(list));
    }

    Control Calendar(Settings s)
    {
        var ics = Input(s.CalendarIcsUrl, "https://calendar.google.com/calendar/ical/…/basic.ics"); ics.TextChanged += async (_, _) => { s.CalendarIcsUrl = ics.Text ?? ""; s.Save(); await CalendarService.Instance.RefreshAsync(); };
        return Page(L.T("Calendar"), L.T("Events from any calendar via an ICS link"),
            Card(Field(L.T("ICS link"), ics), Note("Google Calendar: Settings → your calendar → \"Secret address in iCal format\". Outlook: Publish calendar → ICS. iCloud: Share → Public calendar (webcal link).")));
    }

    Control Weather(Settings s)
    {
        var city = Input(s.WeatherCity, L.T("Automatic (by IP address)")); city.Width = 280;
        var status = new TextBlock { FontSize = 11, Classes = { "muted" }, Text = s.WeatherCity == "" ? L.T("Automatic (by IP address)") : s.WeatherCity };
        var find = new Button { Classes = { "chip" }, Content = L.T("Find") };
        async Task FindCity() { status.Text = "…"; var n = await WeatherService.Instance.SetCityAsync(city.Text ?? ""); status.Text = n ?? ((city.Text ?? "") == "" ? L.T("Automatic (by IP address)") : L.T("Not found")); if (n != null) city.Text = n; }
        find.Click += async (_, _) => await FindCity(); city.KeyDown += async (_, e) => { if (e.Key == Key.Enter) await FindCity(); };
        return Page(L.T("Weather"), L.T("Where to show the forecast for"),
            Card(Field(L.T("Location"), new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { city, find } }), status, Note(L.T("Leave empty for automatic location. Desktop PCs without GPS: type your city (e.g. Praha, Brno)."))));
    }

    Control Transit(Settings s)
    {
        var tok = Input(s.GolemioToken, ""); tok.PasswordChar = '•'; tok.TextChanged += (_, _) => { s.GolemioToken = tok.Text ?? ""; s.Save(); };
        return Page(L.T("Transit"), L.T("Prague departures need a free Golemio token"),
            Card(Field(L.T("Golemio token"), tok), Note("api.golemio.cz/api-keys (free). RegioJet / FlixBus need no key.")));
    }

    Control Bakalari(Settings s)
    {
        var srv = Input(s.BakalariServer, ""); srv.TextChanged += (_, _) => { s.BakalariServer = srv.Text ?? ""; s.Save(); };
        var usr = Input(s.BakalariUser, ""); usr.TextChanged += (_, _) => { s.BakalariUser = usr.Text ?? ""; s.Save(); };
        var pwd = Input(Secure.Unprotect(s.BakalariPassword), ""); pwd.PasswordChar = '•'; pwd.TextChanged += (_, _) => { s.BakalariPassword = Secure.Protect(pwd.Text ?? ""); s.Save(); };
        var cls = new ComboBox { Width = 150 }; var grp = new ComboBox { ItemsSource = new[] { L.T("all"), "1.sk", "2.sk" }, SelectedIndex = s.BakalariGroup, Width = 90 };
        grp.SelectionChanged += (_, _) => { s.BakalariGroup = grp.SelectedIndex; s.Save(); };
        var st = new TextBlock { FontSize = 11, Classes = { "muted" }, TextWrapping = TextWrapping.Wrap };
        void FillClasses() { var b = BakalariService.Instance; cls.ItemsSource = b.Classes.Select(c => c.name).ToList(); cls.SelectedIndex = b.Classes.FindIndex(c => c.id == s.BakalariClass); st.Text = b.Status; }
        cls.SelectionChanged += async (_, _) => { var b = BakalariService.Instance; if (cls.SelectedIndex >= 0 && cls.SelectedIndex < b.Classes.Count) { s.BakalariClass = b.Classes[cls.SelectedIndex].id; s.Save(); await b.RefreshAsync(true); } };
        var load = new Button { Classes = { "chip" }, Content = L.T("Sign in and load classes") }; load.Click += async (_, _) => { await BakalariService.Instance.LoadClassesAsync(); FillClasses(); };
        BakalariService.Instance.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BakalariService.Status)) Avalonia.Threading.Dispatcher.UIThread.Post(() => st.Text = BakalariService.Instance.Status); };
        FillClasses();
        return Page("Bakaláři", L.T("School timetable with breaks and changes"),
            Card(Field(L.T("Server"), srv), Field(L.T("Username"), usr), Field(L.T("Password"), pwd)),
            Card(Field(L.T("Class"), new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { load, cls, grp } }), st));
    }

    Control Shot(Settings s)
    {
        var folder = Input(s.ScreenshotFolder, ""); folder.TextChanged += (_, _) => { s.ScreenshotFolder = folder.Text ?? ""; s.Save(); };
        var keys = new StackPanel { Spacing = 4 };
        foreach (var (k, what) in new[] { ("Ctrl+Shift+2", L.T("Area")), ("Ctrl+Shift+1", L.T("Screen")), ("Ctrl+Shift+O", L.T("Text (OCR)")), ("Ctrl+Shift+V", L.T("Clipboard")) })
            keys.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { new Border { Background = new SolidColorBrush(Color.Parse("#22FFFFFF")), CornerRadius = new CornerRadius(6), Padding = new Thickness(7, 2), Child = new TextBlock { Text = k, FontSize = 11, FontWeight = FontWeight.SemiBold } }, new TextBlock { Text = what, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Classes = { "muted" } } } });
        return Page(L.T("Shot"), L.T("Screenshots are copied and saved to a folder"),
            Card(Field(L.T("Screenshot folder"), folder)), Card(new TextBlock { Text = L.T("Shortcuts"), FontSize = 12, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 6) }, keys));
    }

    // ---------- stavební prvky
    static Control Page(string title, string subtitle, params Control[] cards)
    {
        var st = new StackPanel { Spacing = 12, Children = { new TextBlock { Text = title, FontSize = 22, FontWeight = FontWeight.Bold }, new TextBlock { Text = subtitle, FontSize = 12, Classes = { "muted" }, Margin = new Thickness(0, -8, 0, 4) } } };
        foreach (var c in cards) st.Children.Add(c);
        return st;
    }
    static Control Card(params Control[] items) { var st = new StackPanel { Spacing = 8 }; foreach (var i in items) st.Children.Add(i); return new Border { Background = new SolidColorBrush(Color.Parse("#0FFFFFFF")), BorderBrush = new SolidColorBrush(Color.Parse("#1AFFFFFF")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(14, 12), Child = st }; }
    static Control Row(string title, string desc, Control control, string? icon = null)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), MinHeight = 40 };
        if (icon != null) { var i = FA.Icon(icon, 13, false, new SolidColorBrush(Color.Parse("#C8C8D0"))); i.Width = 28; i.VerticalAlignment = VerticalAlignment.Center; g.Children.Add(i); }
        g.Children.Add(Col(new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeight.Medium }, new TextBlock { Text = desc, FontSize = 11, Classes = { "muted" }, TextWrapping = TextWrapping.Wrap } } }, 1));
        control.VerticalAlignment = VerticalAlignment.Center; g.Children.Add(Col(control, 2));
        return g;
    }
    static TextBox Input(string text, string placeholder) => new() { Classes = { "dark" }, Text = text, PlaceholderText = placeholder, MinHeight = 30, Padding = new Thickness(10, 6) };
    static Control Field(string label, Control c) => new Grid { ColumnDefinitions = new ColumnDefinitions("130,*"), Children = { new TextBlock { Text = label, FontSize = 12, Classes = { "muted" }, VerticalAlignment = VerticalAlignment.Center }, Col(c, 1) } };
    static Control Note(string s) => new TextBlock { Text = s, FontSize = 10.5, Classes = { "dim" }, TextWrapping = TextWrapping.Wrap };
    static Control Col(Control c, int i) { Grid.SetColumn(c, i); return c; }
    static Control Dock(Control c, Avalonia.Controls.Dock d) { DockPanel.SetDock(c, d); return c; }
}
