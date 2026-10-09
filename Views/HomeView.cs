using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NotchIsland.Core;
using NotchIsland.Services;

namespace NotchIsland.Views;

/// Domů: hudba | kalendář + další hodina | počasí
public class HomeView : UserControl
{
    readonly Image art = new() { Stretch = Stretch.UniformToFill };
    readonly TextBlock title = new() { FontSize = 13, FontWeight = FontWeight.Bold }, album = new() { FontSize = 11, Classes = { "muted" } }, artist = new() { FontSize = 11, Classes = { "muted" } };
    readonly TextBlock app = new() { FontSize = 9, Classes = { "dim" } };
    readonly Equalizer eq = new() { Width = 16, Height = 12, Fill = new SolidColorBrush(Color.Parse("#CCFFFFFF")) };
    readonly Button play = new() { Classes = { "icon" }, Content = FA.Icon(FA.Play, 13) };
    readonly TextBlock wTemp = new() { FontSize = 26, FontWeight = FontWeight.Light }, wCity = new() { FontSize = 10, FontWeight = FontWeight.SemiBold }, wDesc = new() { FontSize = 10 }, wHL = new() { FontSize = 10, Opacity = 0.8 }, wIcon = FA.Icon(FA.CloudSun, 22, false, Brushes.Gold);
    readonly Border wTile = new() { CornerRadius = new CornerRadius(14), Padding = new Thickness(10), Width = 150 };
    readonly TextBlock nextLesson = new() { FontSize = 11, FontWeight = FontWeight.Medium };
    readonly StackPanel days = new() { Orientation = Orientation.Horizontal, Spacing = 2 };
    readonly StackPanel evList = new() { Spacing = 2 };
    readonly TextBlock month = new() { FontSize = 16, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock nothing = new() { FontSize = 11, Classes = { "muted" }, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };
    DateTime selected = DateTime.Today; double dragAcc; Point? dragStart;

    public HomeView()
    {
        var np = NowPlayingService.Instance; var w = WeatherService.Instance; var bk = BakalariService.Instance;
        var artBorder = new Border { Width = 78, Height = 78, CornerRadius = new CornerRadius(14), ClipToBounds = true, Background = new SolidColorBrush(Color.Parse("#1AFFFFFF")), Child = art };
        play.Click += (_, _) => np.PlayPause();
        var prev = new Button { Classes = { "icon" }, Content = FA.Icon(FA.Backward, 11) }; prev.Click += (_, _) => np.Previous();
        var next = new Button { Classes = { "icon" }, Content = FA.Icon(FA.Forward, 11) }; next.Click += (_, _) => np.Next();
        var ctl = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 0), Children = { prev, play, next, eq } };
        var musicText = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Children = { title, album, artist, app, ctl } };
        var music = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Width = 300, Children = { artBorder, musicText } };

        // kalendář (dnešní týden) + další hodina
        var cal = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(14, 0) };
        month.PointerPressed += (_, _) => { selected = DateTime.Today; RenderDays(); };
        // tažení po pásu dní = scrubování (jako na Macu)
        var strip = new Border { Background = Brushes.Transparent, Child = days, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
        strip.PointerPressed += (_, e) => { dragStart = e.GetPosition(strip); dragAcc = 0; };
        strip.PointerMoved += (_, e) => { if (dragStart is { } st && e.GetCurrentPoint(strip).Properties.IsLeftButtonPressed) { var dx = e.GetPosition(strip).X - st.X - dragAcc; while (dx <= -46) { selected = selected.AddDays(1); dragAcc -= 46; dx += 46; RenderDays(); } while (dx >= 46) { selected = selected.AddDays(-1); dragAcc += 46; dx -= 46; RenderDays(); } } };
        strip.PointerReleased += (_, _) => dragStart = null;
        strip.PointerWheelChanged += (_, e) => { if (Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y)) { selected = selected.AddDays(e.Delta.X < 0 ? 1 : -1); RenderDays(); } };
        cal.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { month, strip } });
        cal.Children.Add(nextLesson); cal.Children.Add(evList); cal.Children.Add(nothing);
        CalendarService.Instance.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(RenderDays);
        RenderDays();

        wTile.Child = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), Children = {
            Row(new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { wCity, Col(FA.Icon(FA.Location, 8, false, new SolidColorBrush(Color.Parse("#B3FFFFFF"))), 1) } }, 0),
            Row(new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { wTemp, Col(wIcon, 1) } }, 1), Row(wDesc, 2), Row(wHL, 3) } };

        Content = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,1,*,1,Auto"), Children = { Col(music, 0), Col(Sep(), 1), Col(cal, 2), Col(Sep(), 3), Col(wTile, 4) } };

        np.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateMusic);
        w.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateWeather);
        bk.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateLesson);
        UpdateMusic(); UpdateWeather(); UpdateLesson();
        void UpdateLesson() { var t = bk.StateText; nextLesson.Text = t; nextLesson.IsVisible = t != "" && selected == DateTime.Today; RenderEvents(); }
    }

    /// Pásek dní kolem vybraného dne (vybraný uprostřed) + události
    void RenderDays()
    {
        days.Children.Clear();
        month.Text = selected.ToString("MMM", System.Globalization.CultureInfo.CurrentUICulture);
        for (int i = -3; i <= 3; i++)
        {
            var d = selected.AddDays(i); var today = d == DateTime.Today; var sel = i == 0;
            var cell = new StackPanel { Width = 44, Spacing = 0, Children = {
                new TextBlock { Text = d.ToString("ddd", System.Globalization.CultureInfo.CurrentUICulture)[..1].ToUpper(), FontSize = 7, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Opacity = sel ? 1 : 0.4 },
                new TextBlock { Text = d.Day.ToString(), FontSize = sel ? 14 : 12, FontWeight = sel ? FontWeight.Bold : FontWeight.Medium, HorizontalAlignment = HorizontalAlignment.Center, Foreground = sel && today ? new SolidColorBrush(Color.Parse("#3B82F6")) : sel ? Brushes.White : new SolidColorBrush(Color.Parse(today ? "#E6FFFFFF" : "#80FFFFFF")) } } };
            var b = new Border { CornerRadius = new CornerRadius(6), Background = sel ? new SolidColorBrush(Color.Parse("#1FFFFFFF")) : Brushes.Transparent, Child = cell, Padding = new Thickness(0, 2) };
            var dd = d; b.PointerPressed += (_, e) => { if (dragAcc == 0) { selected = dd; RenderDays(); } };
            days.Children.Add(b);
        }
        nextLesson.IsVisible = nextLesson.Text != "" && selected == DateTime.Today;
        RenderEvents();
    }
    void RenderEvents()
    {
        evList.Children.Clear();
        var cs = CalendarService.Instance; var list = cs.On(selected);
        foreach (var e in list.Take(2))
            evList.Children.Add(new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,6,*,Auto"), Children = {
                new Border { Width = 2, Height = 12, CornerRadius = new CornerRadius(1), Background = new SolidColorBrush(Color.Parse("#3B82F6")), VerticalAlignment = VerticalAlignment.Center },
                Col(new TextBlock { Text = e.Title, FontSize = 10, FontWeight = FontWeight.Medium, TextTrimming = TextTrimming.CharacterEllipsis }, 2),
                Col(new TextBlock { Text = e.AllDay ? L.T("all day") : e.Start.ToString("H:mm"), FontSize = 9, Classes = { "muted" } }, 3) } });
        if (list.Count > 2) evList.Children.Add(new TextBlock { Text = $"+{list.Count - 2}", FontSize = 8, Classes = { "dim" } });
        nothing.Text = !cs.IsConfigured ? L.T("Add a calendar ICS link in Settings") : (cs.Status != "" ? cs.Status : L.T("Nothing today"));
        nothing.IsVisible = list.Count == 0 && !nextLesson.IsVisible;
    }
    static Control Col(Control c, int col) { Grid.SetColumn(c, col); return c; }
    static Control Row(Control c, int row) { Grid.SetRow(c, row); return c; }
    static Control Sep() => new Border { Width = 1, Background = new SolidColorBrush(Color.Parse("#1FFFFFFF")), Margin = new Thickness(0, 6) };

    void UpdateMusic()
    {
        var np = NowPlayingService.Instance;
        if (!np.HasMedia) { title.Text = L.T("Nothing playing"); album.Text = "Spotify · " + L.T("Browser"); artist.Text = ""; app.Text = ""; art.Source = null; eq.Active = false; return; }
        title.Text = np.Title; album.Text = string.IsNullOrEmpty(np.Album) ? np.App : np.Album; artist.Text = np.Artist; app.Text = np.App; play.Content = FA.Icon(np.IsPlaying ? FA.Pause : FA.Play, 13); eq.Active = np.IsPlaying;
        if (np.Artwork != null) { try { art.Source = new Bitmap(new MemoryStream(np.Artwork)); } catch { } }
    }
    void UpdateWeather()
    {
        var w = WeatherService.Instance;
        if (!w.Loaded) { wCity.Text = L.T("Locating…"); wTemp.Text = "–"; wTile.Background = new SolidColorBrush(Color.Parse("#1E3A5F")); return; }
        var (a, b) = WeatherService.Gradient(w.Code, w.IsDay);
        wTile.Background = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse(a), 0), new GradientStop(Color.Parse(b), 1) } };
        wCity.Text = w.City; wTemp.Text = $"{Math.Round(w.Temp)}°"; wIcon.Text = FA.Weather(w.Code, w.IsDay); wIcon.Foreground = w.Code == 0 && w.IsDay ? Brushes.Gold : Brushes.White; wDesc.Text = WeatherService.Text(w.Code); wHL.Text = $"H:{Math.Round(w.TMax)}° L:{Math.Round(w.TMin)}°";
    }
}
