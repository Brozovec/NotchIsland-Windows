using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NotchIsland.Core;
using NotchIsland.Services;

namespace NotchIsland.Views;

/// Nastavení jako samostatné okno.
public class SettingsWindow : Window
{
    static SettingsWindow? inst;
    public static void ShowIt() { if (inst == null) { inst = new SettingsWindow(); inst.Closed += (_, _) => inst = null; } inst.Show(); inst.Activate(); }

    SettingsWindow()
    {
        Title = L.T("Settings") + " – NotchIsland"; Width = 560; Height = 640; Background = new SolidColorBrush(Color.Parse("#1C1C20")); CanResize = true;
        var s = Settings.Current;
        var root = new StackPanel { Spacing = 10, Margin = new Thickness(16) };

        // Obecné
        var login = new CheckBox { Content = L.T("Launch at login"), IsChecked = s.LaunchAtLogin }; login.IsCheckedChanged += (_, _) => { s.LaunchAtLogin = login.IsChecked == true; s.Save(); Platform.SetLaunchAtLogin(s.LaunchAtLogin); };
        var lang = new ComboBox { ItemsSource = new[] { L.T("System"), "English", "Čeština" }, SelectedIndex = s.Language == "" ? 0 : s.Language == "cs" ? 2 : 1, Width = 160 };
        lang.SelectionChanged += (_, _) => { s.Language = lang.SelectedIndex switch { 1 => "en", 2 => "cs", _ => "" }; s.Save(); };
        root.Children.Add(Section(L.T("General"), login, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = L.T("Language") + " (restart)", VerticalAlignment = VerticalAlignment.Center }, lang } }, Note("NotchIsland 1.0 · Adam Brož · notchisland.brozovec.eu")));

        // Moduly
        var mods = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var (id, name) in new[] { ("tray", L.T("Tray")), ("transit", L.T("Transit")), ("calls", L.T("Calls")), ("shot", L.T("Shot")), ("notes", L.T("Notes")), ("clipboard", L.T("Clipboard")), ("timer", L.T("Timer")), ("timetable", L.T("Timetable")) })
        {
            var b = new Button { Classes = { "chip" }, Content = name, Margin = new Thickness(0, 0, 6, 6), Opacity = s.DisabledTabs.Contains(id) ? 0.4 : 1 };
            b.Click += (_, _) => { if (s.DisabledTabs.Contains(id)) s.DisabledTabs.Remove(id); else s.DisabledTabs.Add(id); s.Save(); b.Opacity = s.DisabledTabs.Contains(id) ? 0.4 : 1; App.Island?.RebuildTabs(); };
            mods.Children.Add(b);
        }
        root.Children.Add(Section(L.T("Modules"), mods));

        // Kalendář
        var ics = new TextBox { Classes = { "dark" }, Text = s.CalendarIcsUrl, Watermark = "https://calendar.google.com/calendar/ical/…/basic.ics" }; ics.TextChanged += async (_, _) => { s.CalendarIcsUrl = ics.Text ?? ""; s.Save(); await CalendarService.Instance.RefreshAsync(); };
        root.Children.Add(Section(L.T("Calendar (ICS link)"), ics, Note("Google Calendar: Settings → your calendar → \"Secret address in iCal format\". Outlook: Publish calendar → ICS. iCloud: Share → Public calendar (webcal link).")));

        // Doprava
        var tok = new TextBox { Classes = { "dark" }, Text = s.GolemioToken, PasswordChar = '•' }; tok.TextChanged += (_, _) => { s.GolemioToken = tok.Text ?? ""; s.Save(); };
        root.Children.Add(Section(L.T("Transit"), Field(L.T("Golemio token"), tok), Note("api.golemio.cz/api-keys (free). RegioJet / FlixBus need no key.")));

        // Bakaláři
        var srv = new TextBox { Classes = { "dark" }, Text = s.BakalariServer }; srv.TextChanged += (_, _) => { s.BakalariServer = srv.Text ?? ""; s.Save(); };
        var usr = new TextBox { Classes = { "dark" }, Text = s.BakalariUser }; usr.TextChanged += (_, _) => { s.BakalariUser = usr.Text ?? ""; s.Save(); };
        var pwd = new TextBox { Classes = { "dark" }, Text = Secure.Unprotect(s.BakalariPassword), PasswordChar = '•' }; pwd.TextChanged += (_, _) => { s.BakalariPassword = Secure.Protect(pwd.Text ?? ""); s.Save(); };
        var cls = new ComboBox { Width = 140 }; var grp = new ComboBox { ItemsSource = new[] { L.T("all"), "1.sk", "2.sk" }, SelectedIndex = s.BakalariGroup, Width = 90 };
        grp.SelectionChanged += (_, _) => { s.BakalariGroup = grp.SelectedIndex; s.Save(); };
        var st = new TextBlock { FontSize = 11, Classes = { "muted" }, TextWrapping = TextWrapping.Wrap };
        void FillClasses() { var b = BakalariService.Instance; cls.ItemsSource = b.Classes.Select(c => c.name).ToList(); var i = b.Classes.FindIndex(c => c.id == s.BakalariClass); cls.SelectedIndex = i; st.Text = b.Status; }
        cls.SelectionChanged += async (_, _) => { var b = BakalariService.Instance; if (cls.SelectedIndex >= 0 && cls.SelectedIndex < b.Classes.Count) { s.BakalariClass = b.Classes[cls.SelectedIndex].id; s.Save(); await b.RefreshAsync(true); } };
        var load = new Button { Classes = { "chip" }, Content = L.T("Sign in and load classes") }; load.Click += async (_, _) => { await BakalariService.Instance.LoadClassesAsync(); FillClasses(); };
        BakalariService.Instance.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BakalariService.Status)) Avalonia.Threading.Dispatcher.UIThread.Post(() => st.Text = BakalariService.Instance.Status); };
        FillClasses();
        root.Children.Add(Section("Bakaláři", Field(L.T("Server"), srv), Field(L.T("Username"), usr), Field(L.T("Password"), pwd), new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { load, cls, grp } }, st));

        // Screenshoty
        var folder = new TextBox { Classes = { "dark" }, Text = s.ScreenshotFolder }; folder.TextChanged += (_, _) => { s.ScreenshotFolder = folder.Text ?? ""; s.Save(); };
        root.Children.Add(Section(L.T("Shot"), Field(L.T("Screenshot folder"), folder), Note("Ctrl+Shift+2 area · Ctrl+Shift+1 screen · Ctrl+Shift+O text (OCR) · Ctrl+Shift+V clipboard")));
        Content = new ScrollViewer { Content = root };
    }
    static Control Section(string title, params Control[] items) { var st = new StackPanel { Spacing = 6, Children = { new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeight.Bold } } }; foreach (var i in items) st.Children.Add(i); return new Border { Classes = { "tile" }, Child = st }; }
    static Control Field(string label, Control c) => new Grid { ColumnDefinitions = new ColumnDefinitions("110,*"), Children = { new TextBlock { Text = label, FontSize = 12, Classes = { "muted" }, VerticalAlignment = VerticalAlignment.Center }, Col(c, 1) } };
    static Control Note(string s) => new TextBlock { Text = s, FontSize = 10, Classes = { "dim" }, TextWrapping = TextWrapping.Wrap };
    static Control Col(Control c, int i) { Grid.SetColumn(c, i); return c; }
}
