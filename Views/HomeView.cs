using Avalonia;
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
    readonly Button play = new() { Classes = { "icon" }, Content = "▶" };
    readonly TextBlock wTemp = new() { FontSize = 26, FontWeight = FontWeight.Light }, wCity = new() { FontSize = 10, FontWeight = FontWeight.SemiBold }, wDesc = new() { FontSize = 10 }, wHL = new() { FontSize = 10, Opacity = 0.8 }, wIcon = new() { FontSize = 22 };
    readonly Border wTile = new() { CornerRadius = new CornerRadius(14), Padding = new Thickness(10), Width = 150 };
    readonly TextBlock nextLesson = new() { FontSize = 11, FontWeight = FontWeight.Medium };
    readonly StackPanel days = new() { Orientation = Orientation.Horizontal, Spacing = 2 };

    public HomeView()
    {
        var np = NowPlayingService.Instance; var w = WeatherService.Instance; var bk = BakalariService.Instance;
        var artBorder = new Border { Width = 78, Height = 78, CornerRadius = new CornerRadius(14), ClipToBounds = true, Background = new SolidColorBrush(Color.Parse("#1AFFFFFF")), Child = art };
        play.Click += (_, _) => np.PlayPause();
        var prev = new Button { Classes = { "icon" }, Content = "⏮" }; prev.Click += (_, _) => np.Previous();
        var next = new Button { Classes = { "icon" }, Content = "⏭" }; next.Click += (_, _) => np.Next();
        var ctl = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 0), Children = { prev, play, next, eq } };
        var musicText = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Children = { title, album, artist, app, ctl } };
        var music = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Width = 300, Children = { artBorder, musicText } };

        // kalendář (dnešní týden) + další hodina
        var cal = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(14, 0) };
        var month = new TextBlock { Text = DateTime.Today.ToString("MMM", System.Globalization.CultureInfo.CurrentUICulture), FontSize = 16, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
        var monday = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        for (int i = 0; i < 7; i++)
        {
            var d = monday.AddDays(i); var today = d == DateTime.Today;
            var cell = new StackPanel { Width = 44, Spacing = 0, Children = {
                new TextBlock { Text = d.ToString("ddd", System.Globalization.CultureInfo.CurrentUICulture)[..1].ToUpper(), FontSize = 7, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Opacity = today ? 1 : 0.4 },
                new TextBlock { Text = d.Day.ToString(), FontSize = today ? 14 : 12, FontWeight = today ? FontWeight.Bold : FontWeight.Medium, HorizontalAlignment = HorizontalAlignment.Center, Foreground = today ? new SolidColorBrush(Color.Parse("#3B82F6")) : new SolidColorBrush(Color.Parse("#99FFFFFF")) } } };
            days.Children.Add(new Border { CornerRadius = new CornerRadius(6), Background = today ? new SolidColorBrush(Color.Parse("#1FFFFFFF")) : Brushes.Transparent, Child = cell, Padding = new Thickness(0, 2) });
        }
        cal.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { month, days } });
        var nothing = new TextBlock { Text = L.T("Nothing today"), FontSize = 11, Classes = { "muted" }, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        cal.Children.Add(nextLesson); cal.Children.Add(nothing);

        wTile.Child = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), Children = {
            Row(new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { wCity, Col(new TextBlock { Text = "➤", FontSize = 8, Opacity = 0.7 }, 1) } }, 0),
            Row(new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { wTemp, Col(wIcon, 1) } }, 1), Row(wDesc, 2), Row(wHL, 3) } };

        Content = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,1,*,1,Auto"), Children = { Col(music, 0), Col(Sep(), 1), Col(cal, 2), Col(Sep(), 3), Col(wTile, 4) } };

        np.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateMusic);
        w.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateWeather);
        bk.PropertyChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateLesson);
        UpdateMusic(); UpdateWeather(); UpdateLesson();
        void UpdateLesson() { var t = bk.StateText; nextLesson.Text = t == "" ? "" : "🎓 " + t; nextLesson.IsVisible = t != ""; nothing.IsVisible = t == ""; }
    }
    static Control Col(Control c, int col) { Grid.SetColumn(c, col); return c; }
    static Control Row(Control c, int row) { Grid.SetRow(c, row); return c; }
    static Control Sep() => new Border { Width = 1, Background = new SolidColorBrush(Color.Parse("#1FFFFFFF")), Margin = new Thickness(0, 6) };

    void UpdateMusic()
    {
        var np = NowPlayingService.Instance;
        if (!np.HasMedia) { title.Text = L.T("Nothing playing"); album.Text = "Spotify · " + L.T("Browser"); artist.Text = ""; app.Text = ""; art.Source = null; eq.Active = false; return; }
        title.Text = np.Title; album.Text = string.IsNullOrEmpty(np.Album) ? np.App : np.Album; artist.Text = np.Artist; app.Text = np.App; play.Content = np.IsPlaying ? "⏸" : "▶"; eq.Active = np.IsPlaying;
        if (np.Artwork != null) { try { art.Source = new Bitmap(new MemoryStream(np.Artwork)); } catch { } }
    }
    void UpdateWeather()
    {
        var w = WeatherService.Instance;
        if (!w.Loaded) { wCity.Text = L.T("Locating…"); wTemp.Text = "–"; wTile.Background = new SolidColorBrush(Color.Parse("#1E3A5F")); return; }
        var (a, b) = WeatherService.Gradient(w.Code, w.IsDay);
        wTile.Background = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse(a), 0), new GradientStop(Color.Parse(b), 1) } };
        wCity.Text = w.City; wTemp.Text = $"{Math.Round(w.Temp)}°"; wIcon.Text = WeatherService.Icon(w.Code, w.IsDay); wDesc.Text = WeatherService.Text(w.Code); wHL.Text = $"H:{Math.Round(w.TMax)}° L:{Math.Round(w.TMin)}°";
    }
}
