using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

public record CalEvent(string Title, DateTime Start, DateTime End, bool AllDay, string? Location);

/// Kalendář přes ICS odkaz (Google Calendar: Nastavení kalendáře → Tajná adresa ve formátu iCal; Outlook: Publikovat kalendář; iCloud: Sdílet veřejně).
public sealed partial class CalendarService : ObservableObject
{
    public static CalendarService Instance { get; } = new();
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
    [ObservableProperty] List<CalEvent> events = new();
    [ObservableProperty] string status = "";
    public bool IsConfigured => Settings.Current.CalendarIcsUrl.Trim() != "";

    CalendarService()
    {
        var t = new System.Timers.Timer(600_000); t.Elapsed += async (_, _) => await RefreshAsync(); t.Start();
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var url = Settings.Current.CalendarIcsUrl.Trim().Replace("webcal://", "https://");
        if (url == "") { Events = new(); return; }
        try
        {
            var ics = await Http.GetStringAsync(url);
            var list = Parse(ics).Where(e => e.End >= DateTime.Today.AddDays(-7) && e.Start <= DateTime.Today.AddDays(60)).OrderBy(e => e.Start).ToList();
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { Events = list; Status = ""; });
        }
        catch (Exception e) { Avalonia.Threading.Dispatcher.UIThread.Post(() => Status = "Calendar: " + e.Message); Log.W("calendar: " + e.Message); }
    }

    public List<CalEvent> On(DateTime day) => Events.Where(e => e.Start < day.Date.AddDays(1) && e.End > day.Date && !(e.AllDay && e.End == day.Date)).ToList();

    /// Minimalistický parser VEVENT: DTSTART/DTEND (datum i datum-čas, UTC i lokální), SUMMARY, LOCATION; jednoduché RRULE (DAILY/WEEKLY/MONTHLY/YEARLY bez výjimek).
    static List<CalEvent> Parse(string ics)
    {
        var lines = new List<string>();
        foreach (var raw in ics.Replace("\r\n", "\n").Split('\n')) { if (raw.StartsWith(" ") || raw.StartsWith("\t")) { if (lines.Count > 0) lines[^1] += raw[1..]; } else lines.Add(raw); }
        var result = new List<CalEvent>(); bool inEv = false; var f = new Dictionary<string, string>();
        foreach (var l in lines)
        {
            if (l == "BEGIN:VEVENT") { inEv = true; f.Clear(); continue; }
            if (l == "END:VEVENT") { inEv = false; Emit(f, result); continue; }
            if (!inEv) continue;
            var i = l.IndexOf(':'); if (i < 0) continue;
            var key = l[..i]; var val = l[(i + 1)..];
            var name = key.Split(';')[0];
            f[name] = val; f[name + ";"] = key;   // parametry (TZID, VALUE=DATE)
        }
        return result;
    }
    static DateTime? Dt(Dictionary<string, string> f, string name, out bool allDay)
    {
        allDay = false;
        if (!f.TryGetValue(name, out var v)) return null;
        var p = f.GetValueOrDefault(name + ";", "");
        if (v.Length == 8 || p.Contains("VALUE=DATE")) { allDay = true; return DateTime.TryParseExact(v[..8], "yyyyMMdd", null, DateTimeStyles.None, out var d) ? d : null; }
        var utc = v.EndsWith("Z"); var s = v.TrimEnd('Z');
        if (!DateTime.TryParseExact(s, "yyyyMMddTHHmmss", null, DateTimeStyles.None, out var dt)) return null;
        if (utc) return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
        var m = Regex.Match(p, "TZID=([^;:]+)");
        if (m.Success) { try { var tz = TimeZoneInfo.FindSystemTimeZoneById(m.Groups[1].Value); return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), tz, TimeZoneInfo.Local); } catch { } }
        return dt;
    }
    static void Emit(Dictionary<string, string> f, List<CalEvent> result)
    {
        var start = Dt(f, "DTSTART", out var allDay); if (start == null) return;
        var end = Dt(f, "DTEND", out _) ?? (allDay ? start.Value.AddDays(1) : start.Value.AddHours(1));
        var title = (f.GetValueOrDefault("SUMMARY", "(untitled)")).Replace("\\,", ",").Replace("\\n", " ");
        var loc = f.TryGetValue("LOCATION", out var lo) && lo != "" ? lo.Replace("\\,", ",") : null;
        var ev = new CalEvent(title, start.Value, end, allDay, loc);
        result.Add(ev);
        if (f.TryGetValue("RRULE", out var rr))
        {
            var freq = Regex.Match(rr, "FREQ=(\\w+)").Groups[1].Value; var interval = int.TryParse(Regex.Match(rr, "INTERVAL=(\\d+)").Groups[1].Value, out var iv) ? iv : 1;
            var until = Regex.Match(rr, "UNTIL=(\\d{8})").Groups[1].Value is { Length: 8 } u && DateTime.TryParseExact(u, "yyyyMMdd", null, DateTimeStyles.None, out var ud) ? ud : DateTime.Today.AddDays(60);
            var count = int.TryParse(Regex.Match(rr, "COUNT=(\\d+)").Groups[1].Value, out var cn) ? cn : 400;
            var byday = Regex.Match(rr, "BYDAY=([A-Z,]+)").Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var dur = end - start.Value; var cur = start.Value; int n = 1;
            while (n < count && cur <= until && cur <= DateTime.Today.AddDays(60))
            {
                cur = freq switch { "DAILY" => cur.AddDays(interval), "WEEKLY" => cur.AddDays(7 * interval), "MONTHLY" => cur.AddMonths(interval), "YEARLY" => cur.AddYears(interval), _ => until.AddDays(1) };
                if (freq == "WEEKLY" && byday.Length > 1) { for (int d = 0; d < 7; d++) { var day = cur.AddDays(-((int)cur.DayOfWeek + 6) % 7 + d); var code = day.DayOfWeek.ToString()[..2].ToUpper(); if (byday.Contains(code) && day > start.Value && day <= until) result.Add(ev with { Start = day, End = day + dur }); } }
                else if (cur <= until) result.Add(ev with { Start = cur, End = cur + dur });
                n++;
            }
        }
    }
}
