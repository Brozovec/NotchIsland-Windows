using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NotchIsland.Core;
using NotchIsland.Services;

namespace NotchIsland.Views;

/// Doprava: režim Zastávka (PID) / Spojení (RegioJet, FlixBus).
public class TransitView : UserControl
{
    readonly TransitService t = TransitService.Instance;
    readonly StackPanel list = new() { Spacing = 0 };
    readonly TextBlock status = new() { Classes = { "muted" }, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    readonly Button modeStop = new() { Classes = { "pill", "on" } }, modeRoute = new() { Classes = { "pill" } };
    readonly Panel header = new();
    bool routes;

    public TransitView()
    {
        modeStop.Content = FA.Label(FA.Tram, L.T("Stop"), 11); modeRoute.Content = FA.Label(FA.Bus, L.T("Routes"), 11);
        modeStop.Click += (_, _) => SetMode(false); modeRoute.Click += (_, _) => SetMode(true);
        var modes = new StackPanel { Spacing = 4, Width = 96, Children = { modeStop, modeRoute } };
        var right = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Children = { header, Row(new Grid { Children = { status, new ScrollViewer { Content = list } } }, 1) } };
        Content = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,8,*"), Children = { modes, Col(right, 2) } };
        t.PropertyChanged += (_, e) => Avalonia.Threading.Dispatcher.UIThread.Post(Render);
        SetMode(false); _ = t.RefreshStopAsync();
    }
    static Control Col(Control c, int i) { Grid.SetColumn(c, i); return c; }
    static Control Row(Control c, int i) { Grid.SetRow(c, i); return c; }

    void SetMode(bool r)
    {
        routes = r; modeStop.Classes.Set("on", !r); modeRoute.Classes.Set("on", r);
        header.Children.Clear();
        if (!r)
        {
            var box = new TextBox { Classes = { "dark" }, Watermark = L.T("Enter a stop"), Text = t.StopQuery };
            box.KeyDown += async (_, e) => { if (e.Key == Avalonia.Input.Key.Enter) { t.StopQuery = box.Text ?? ""; await t.RefreshStopAsync(); } };
            var fav = new Button { Classes = { "icon" }, Content = FA.Icon(FA.Star, 11, false, Brushes.Gold) }; fav.Click += (_, _) => { var s = Settings.Current; var q = (box.Text ?? "").Trim(); if (q == "") return; if (s.FavoriteStops.Contains(q)) s.FavoriteStops.Remove(q); else s.FavoriteStops.Add(q); s.Save(); SetMode(false); };
            var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            foreach (var s in Settings.Current.FavoriteStops) { var c = new Button { Classes = { "chip" }, Content = s, FontSize = 10 }; c.Click += async (_, _) => { box.Text = s; t.StopQuery = s; await t.RefreshStopAsync(); }; chips.Children.Add(c); }
            header.Children.Add(new StackPanel { Spacing = 4, Children = { new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { box, Col(fav, 1) } }, chips } });
        }
        else
        {
            var from = new TextBox { Classes = { "dark" }, Watermark = L.T("From"), Text = t.From }; var to = new TextBox { Classes = { "dark" }, Watermark = L.T("To"), Text = t.To };
            async Task Go() { t.From = from.Text ?? ""; t.To = to.Text ?? ""; await t.SearchAsync(); }
            from.KeyDown += async (_, e) => { if (e.Key == Avalonia.Input.Key.Enter) await Go(); }; to.KeyDown += async (_, e) => { if (e.Key == Avalonia.Input.Key.Enter) await Go(); };
            var swap = new Button { Classes = { "icon" }, Content = FA.Icon(FA.Swap, 10) }; swap.Click += async (_, _) => { (from.Text, to.Text) = (to.Text, from.Text); await Go(); };
            var go = new Button { Classes = { "chip" }, Content = FA.Icon(FA.Search, 11) }; go.Click += async (_, _) => await Go();
            var idos = new Button { Classes = { "chip" }, Content = "ČD · IDOS", Background = new SolidColorBrush(Color.Parse("#0A3D91")) }; idos.Click += (_, _) => Platform.OpenUrl(t.IdosUrl());
            var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            foreach (var r2 in Settings.Current.FavoriteRoutes) { var p = r2.Split('|'); if (p.Length != 2) continue; var c = new Button { Classes = { "chip" }, Content = $"{p[0]} → {p[1]}", FontSize = 10 }; c.Click += async (_, _) => { from.Text = p[0]; to.Text = p[1]; await Go(); }; chips.Children.Add(c); }
            var fav = new Button { Classes = { "icon" }, Content = FA.Icon(FA.Star, 11, false, Brushes.Gold) }; fav.Click += (_, _) => { var s = Settings.Current; var k = $"{from.Text}|{to.Text}"; if (s.FavoriteRoutes.Contains(k)) s.FavoriteRoutes.Remove(k); else s.FavoriteRoutes.Add(k); s.Save(); SetMode(true); };
            header.Children.Add(new StackPanel { Spacing = 4, Children = { new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto,Auto,Auto"), Children = { from, Col(swap, 1), Col(to, 2), Col(fav, 3), Col(go, 4), Col(idos, 5) } }, chips } });
        }
        Render();
    }

    void Render()
    {
        list.Children.Clear();
        if (!routes)
        {
            status.Text = t.StopStatus; status.IsVisible = t.Departures.Count == 0;
            foreach (var d in t.Departures)
            {
                var badge = new Border { Background = new SolidColorBrush(Color.Parse(d.Color)), CornerRadius = new CornerRadius(5), Padding = new Thickness(5, 2), MinWidth = 48, Child = FA.Label(d.RouteType switch { 0 => FA.Tram, 1 => FA.Train, 2 => FA.TrainSide, _ => FA.Bus }, d.Line, 10) };
                var txt = new StackPanel { Children = { new TextBlock { Text = d.Headsign, FontSize = 11, FontWeight = FontWeight.Medium }, new TextBlock { Text = d.Sub, FontSize = 9, Classes = { "muted" } } } };
                var delay = new TextBlock { Text = d.DelayText, FontSize = 10, FontWeight = FontWeight.SemiBold, Foreground = d.DelayMin > 0 ? Brushes.OrangeRed : Brushes.LightGreen, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
                var mins = new TextBlock { Text = d.MinutesText, FontSize = 12, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, Width = 50, TextAlignment = TextAlignment.Right };
                list.Children.Add(new Border { Classes = { "row" }, Child = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,8,*,Auto,Auto"), Children = { badge, Col(txt, 2), Col(delay, 3), Col(mins, 4) } } });
            }
        }
        else
        {
            status.Text = t.RouteStatus == "" ? L.T("Enter from and to, searches today's connections") : t.RouteStatus; status.IsVisible = t.Connections.Count == 0;
            foreach (var c in t.Connections)
            {
                var badge = new Border { Background = new SolidColorBrush(Color.Parse(c.CarrierColor)), CornerRadius = new CornerRadius(5), Padding = new Thickness(6, 2), Width = 72, Child = FA.Label(c.IsTrain ? FA.TrainSide : FA.Bus, c.Carrier, 9, false, new SolidColorBrush(Color.Parse(c.CarrierFg))) };
                var times = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { new TextBlock { Text = c.Times, FontSize = 11, FontWeight = FontWeight.Bold }, new TextBlock { Text = c.Dur, FontSize = 9, Classes = { "muted" }, VerticalAlignment = VerticalAlignment.Center } } };
                if (c.Transfers > 0) times.Children.Add(new TextBlock { Text = $"· {c.Transfers}×", FontSize = 9, Classes = { "muted" }, VerticalAlignment = VerticalAlignment.Center });
                var txt = new StackPanel { Children = { times, new TextBlock { Text = $"{c.From} → {c.To}", FontSize = 9, Classes = { "muted" } } } };
                var right = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Children = { new TextBlock { Text = c.Live, FontSize = 9, FontWeight = FontWeight.SemiBold, Foreground = c.DelayMin > 0 ? Brushes.OrangeRed : Brushes.LightGreen, TextAlignment = TextAlignment.Right }, new TextBlock { Text = c.Extra, FontSize = 10, TextAlignment = TextAlignment.Right } } };
                var buy = new Button { Classes = { "icon" }, Content = FA.Icon(FA.Cart, 11, false, new SolidColorBrush(Color.Parse("#99FFFFFF"))) }; ToolTip.SetTip(buy, L.T("Buy ticket")); if (c.BuyUrl != null) buy.Click += (_, _) => Platform.OpenUrl(c.BuyUrl);
                list.Children.Add(new Border { Classes = { "row" }, Child = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,8,*,Auto,Auto"), Children = { badge, Col(txt, 2), Col(right, 3), Col(buy, 4) } } });
            }
        }
    }
}
