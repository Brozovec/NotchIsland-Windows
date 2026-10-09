using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using NotchIsland.Core;
using NotchIsland.Services;

namespace NotchIsland.Views;

/// Rozvrh: Dnes (karty 0–12) / Týden (mřížka), stav přestávky, detail po najetí.
public class TimetableView : UserControl
{
    readonly BakalariService b = BakalariService.Instance;
    DateTime day = DateTime.Today; bool week;
    readonly Panel body = new(); readonly TextBlock title = new() { FontSize = 12, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center }, src = new() { FontSize = 9, Classes = { "dim" }, VerticalAlignment = VerticalAlignment.Center }, state = new() { FontSize = 11, FontWeight = FontWeight.Medium };
    readonly Button bDay = new() { Classes = { "pill", "on" } }, bWeek = new() { Classes = { "pill" } };

    public TimetableView()
    {
        bDay.Content = L.T("Today"); bWeek.Content = L.T("Week");
        bDay.Click += (_, _) => { week = false; Render(); }; bWeek.Click += (_, _) => { week = true; Render(); };
        var prev = new Button { Classes = { "icon" }, Content = FA.Icon(FA.ChevronLeft, 9) }; prev.Click += (_, _) => { day = day.AddDays(week ? -7 : -1); Render(); };
        var next = new Button { Classes = { "icon" }, Content = FA.Icon(FA.ChevronRight, 9) }; next.Click += (_, _) => { day = day.AddDays(week ? 7 : 1); Render(); };
        var refresh = new Button { Classes = { "icon" }, Content = FA.Icon(FA.Rotate, 10) }; refresh.Click += async (_, _) => await b.RefreshAsync(true);
        title.PointerPressed += (_, _) => { day = DateTime.Today; Render(); };
        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,*,Auto,Auto"), Children = { bDay, Col(bWeek, 1), Col(prev, 2), Col(title, 3), Col(src, 4), Col(next, 5), Col(new TextBlock { Text = Settings.Current.BakalariClass, FontSize = 9, FontWeight = FontWeight.Bold, Classes = { "dim" }, VerticalAlignment = VerticalAlignment.Center }, 7), Col(refresh, 8) } };
        Content = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), Children = { head, Row(state, 1), Row(body, 2) } };
        b.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(b.Actual) or nameof(b.Next) or nameof(b.Permanent) or nameof(b.Status)) Avalonia.Threading.Dispatcher.UIThread.Post(Render); else if (e.PropertyName == nameof(b.StateText)) Avalonia.Threading.Dispatcher.UIThread.Post(() => { if (!week && day == DateTime.Today) state.Text = b.StateText; }); };
        Render();
    }
    static Control Col(Control c, int i) { Grid.SetColumn(c, i); return c; }
    static Control Row(Control c, int i) { Grid.SetRow(c, i); return c; }

    void Render()
    {
        bDay.Classes.Set("on", !week); bWeek.Classes.Set("on", week);
        var monday = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        title.Text = week ? $"{L.T("Week")} {monday:d. M.}" : day.ToString("dddd d. M.", System.Globalization.CultureInfo.CurrentUICulture);
        var s = b.Source(day); src.Text = s == "" ? "" : "· " + L.T(s);
        body.Children.Clear(); state.Text = "";
        if (!b.IsConfigured) { body.Children.Add(Center(L.T("Fill in Bakaláři in Settings"))); return; }
        if (b.Actual == null) { body.Children.Add(Center(b.Status == "" ? "…" : b.Status)); return; }
        App.Island?.Show("timetable");
        if (week) { body.Children.Add(WeekGrid(monday)); return; }
        var list = b.Lessons(day);
        if (list.Count == 0) { body.Children.Add(Center(L.T("No lessons today"))); return; }
        if (day == DateTime.Today) state.Text = b.StateText;
        var strip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        foreach (var h in b.AllHours)
        {
            var ls = list.Where(l => l.Hour.Id == h.Id).ToList();
            if (ls.Count == 0) { strip.Children.Add(new Border { Width = 66, Height = 76, CornerRadius = new CornerRadius(9), Background = new SolidColorBrush(Color.Parse("#06FFFFFF")), BorderBrush = new SolidColorBrush(Color.Parse("#12FFFFFF")), BorderThickness = new Thickness(1), Child = new Grid { Children = { new TextBlock { Text = h.Id.ToString(), FontSize = 8, FontWeight = FontWeight.Bold, Classes = { "dim" }, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) }, new TextBlock { Text = h.Begin, FontSize = 8, Classes = { "dim" }, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 4) } } } }); continue; }
            var col = new StackPanel { Spacing = 2, Height = 76, Width = 66 };
            foreach (var l in ls) col.Children.Add(Card(l, ls.Count > 1));
            strip.Children.Add(col);
        }
        body.Children.Add(new ScrollViewer { Content = strip, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });
    }

    Control Card(TodayLesson tl, bool compact)
    {
        var l = tl.Lesson; var now = DateTime.Now; var isNow = tl.Start <= now && tl.End > now;
        var bg = l.IsCancelled ? "#24FF3B30" : isNow ? "#383FE36F" : l.IsChanged ? "#33FF9F0A" : "#12FFFFFF";
        var border = l.IsCancelled ? "#80FF3B30" : isNow ? "#993FE36F" : l.IsChanged ? "#80FF9F0A" : "#00000000";
        var st = new StackPanel { Spacing = compact ? 0 : 2, VerticalAlignment = VerticalAlignment.Center };
        if (!compact) st.Children.Add(new TextBlock { Text = tl.Hour.Id.ToString(), FontSize = 8, FontWeight = FontWeight.Bold, Classes = { "dim" }, HorizontalAlignment = HorizontalAlignment.Center });
        var abbr = new TextBlock { Text = (compact && l.Group != null ? l.Group + " " : "") + l.Abbrev, FontSize = compact ? 11 : 13, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center, TextDecorations = l.IsCancelled ? TextDecorations.Strikethrough : null, Opacity = l.IsCancelled ? 0.5 : 1 };
        st.Children.Add(abbr);
        st.Children.Add(l.IsCancelled ? new TextBlock { Text = L.T("Cancelled"), FontSize = 9, Foreground = Brushes.OrangeRed, HorizontalAlignment = HorizontalAlignment.Center } : new TextBlock { Text = l.Room ?? "–", FontSize = compact ? 9 : 10, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse(l.RoomChanged ? "#FF9F0A" : "#5AC8FA")), HorizontalAlignment = HorizontalAlignment.Center });
        if (!compact) { st.Children.Add(new TextBlock { Text = l.Teacher ?? "", FontSize = 8, Classes = { "muted" }, HorizontalAlignment = HorizontalAlignment.Center }); st.Children.Add(new TextBlock { Text = tl.Hour.Begin, FontSize = 8, Classes = { "dim" }, HorizontalAlignment = HorizontalAlignment.Center }); }
        var card = new Border { Background = new SolidColorBrush(Color.Parse(bg)), BorderBrush = new SolidColorBrush(Color.Parse(border)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Child = st, VerticalAlignment = VerticalAlignment.Stretch };
        if (compact) card.Height = 37; else card.Height = 76;
        var tip = $"{tl.Hour.Id}. {tl.Hour.Begin}–{tl.Hour.End}  {l.Name}\n{l.TeacherFull} · {l.RoomFull} · {l.Group}" + (string.IsNullOrEmpty(l.Theme) ? "" : $"\n{l.Theme}") + (string.IsNullOrEmpty(l.Notice) ? "" : $"\n⚠ {l.Notice}") + (l.Change == null ? "" : $"\n{l.Change}");
        ToolTip.SetTip(card, tip);
        card.PointerEntered += (_, _) => state.Text = tip.Replace("\n", "  ·  "); card.PointerExited += (_, _) => state.Text = day == DateTime.Today ? b.StateText : "";
        return card;
    }

    Control WeekGrid(DateTime monday)
    {
        var t = b.For(day)!; var hours = b.AllHours; var g = Settings.Current.BakalariGroup;
        var grid = new Grid { RowSpacing = 3, ColumnSpacing = 3 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); foreach (var _ in hours) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); for (int d = 0; d < 5; d++) grid.RowDefinitions.Add(new RowDefinition(new GridLength(30)));
        for (int i = 0; i < hours.Count; i++) { var h = new TextBlock { Text = hours[i].Id.ToString(), FontSize = 8, FontWeight = FontWeight.Bold, Classes = { "dim" }, HorizontalAlignment = HorizontalAlignment.Center }; Grid.SetColumn(h, i + 1); grid.Children.Add(h); }
        var names = new[] { "Po", "Út", "St", "Čt", "Pá" }; var todayIdx = monday == DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7)) ? ((int)DateTime.Today.DayOfWeek + 6) % 7 : -1;
        for (int d = 0; d < 5; d++)
        {
            var dn = new Border { Width = 24, CornerRadius = new CornerRadius(6), Background = d == todayIdx ? new SolidColorBrush(Color.Parse("#265AC8FA")) : Brushes.Transparent, Child = new TextBlock { Text = names[d], FontSize = 9, FontWeight = FontWeight.Bold, Foreground = d == todayIdx ? new SolidColorBrush(Color.Parse("#5AC8FA")) : new SolidColorBrush(Color.Parse("#8A8A96")), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            Grid.SetRow(dn, d + 1); grid.Children.Add(dn);
            for (int i = 0; i < hours.Count; i++)
            {
                var ls = t.Lessons.Where(l => l.Day == d && l.Hour == hours[i].Id && (g == 0 || l.Group == null || !char.IsDigit(l.Group[0]) || l.Group[0] - '0' == g)).ToList();
                Control cell;
                if (ls.Count == 0) cell = new Border { CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.Parse("#08FFFFFF")) };
                else
                {
                    var st = new StackPanel { Spacing = 1 };
                    foreach (var l in ls.Take(2))
                    {
                        var bg = l.IsCancelled ? "#24FF3B30" : l.IsChanged ? "#38FF9F0A" : d == todayIdx ? "#28FFFFFF" : "#16FFFFFF";
                        var inner = ls.Count > 1 ? (Control)new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, HorizontalAlignment = HorizontalAlignment.Center, Children = { new TextBlock { Text = l.Abbrev, FontSize = 8, FontWeight = FontWeight.Bold }, new TextBlock { Text = l.IsCancelled ? "×" : l.Room, FontSize = 7, Foreground = new SolidColorBrush(Color.Parse(l.IsCancelled ? "#FF3B30" : l.RoomChanged ? "#FF9F0A" : "#5AC8FA")) } } }
                            : new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Children = { new TextBlock { Text = l.Abbrev, FontSize = 10, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center, TextDecorations = l.IsCancelled ? TextDecorations.Strikethrough : null }, new TextBlock { Text = l.IsCancelled ? L.T("Cancelled") : l.Room, FontSize = 8, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.Parse(l.IsCancelled ? "#FF3B30" : l.RoomChanged ? "#FF9F0A" : "#5AC8FA")) } } };
                        var cb = new Border { CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Color.Parse(bg)), Child = inner, VerticalAlignment = VerticalAlignment.Stretch, Height = ls.Count > 1 ? 14 : 30 };
                        ToolTip.SetTip(cb, $"{l.Name} · {l.RoomFull} · {l.TeacherFull}" + (l.Change == null ? "" : $"\n{l.Change}"));
                        st.Children.Add(cb);
                    }
                    cell = st;
                }
                Grid.SetRow(cell, d + 1); Grid.SetColumn(cell, i + 1); grid.Children.Add(cell);
            }
        }
        return grid;
    }
    static Control Center(string s) => new TextBlock { Text = s, Classes = { "muted" }, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
}
