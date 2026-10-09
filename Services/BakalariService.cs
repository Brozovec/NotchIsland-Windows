using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

public record Lesson(int Day, int Hour, string Abbrev, string Name, string? Teacher, string? TeacherFull, string? Room, string? RoomFull, string? Group, string? Theme, string? Notice, string State, string? Change)
{
    public bool IsCancelled => State == "removed" || State == "absent";
    public bool IsChanged => State == "changed" || State == "added";
    public bool RoomChanged => IsChanged && Change != null && Room != null && Room != "" && Change.Contains(Room);
}
public record HourRef(int Id, string Begin, string End);
public record Timetable(DateTime WeekStart, List<HourRef> Hours, List<Lesson> Lessons, DateTime FetchedAt);
public record TodayLesson(HourRef Hour, Lesson Lesson, DateTime Start, DateTime End);

/// Rozvrh z Bakalářů: přihlášení cookie session, JSON `timetableData` ve stránce (nové rozhraní 2026).
public sealed partial class BakalariService : ObservableObject
{
    public static BakalariService Instance { get; } = new();
    readonly CookieContainer cookies = new();
    readonly HttpClient http;
    [ObservableProperty] Timetable? actual;
    [ObservableProperty] Timetable? next;
    [ObservableProperty] Timetable? permanent;
    [ObservableProperty] List<(string id, string name)> classes = new();
    [ObservableProperty] string status = "";
    [ObservableProperty] bool loading;
    [ObservableProperty] string stateText = "";
    [ObservableProperty] string wingText = "";
    [ObservableProperty] bool isBreak;
    bool loggedIn; string notified = "";
    public event Action<string, string>? Notify;

    public static readonly Dictionary<int, (string, string)> SchoolHours = new() { [0] = ("7:10", "7:55"), [1] = ("8:00", "8:45"), [2] = ("8:50", "9:35"), [3] = ("9:45", "10:30"), [4] = ("10:50", "11:35"), [5] = ("11:40", "12:25"), [6] = ("12:35", "13:20"), [7] = ("13:25", "14:10"), [8] = ("14:20", "15:05"), [9] = ("15:10", "15:55"), [10] = ("16:00", "16:45"), [11] = ("16:50", "17:35"), [12] = ("17:40", "18:25") };

    BakalariService()
    {
        http = new HttpClient(new HttpClientHandler { CookieContainer = cookies, UseCookies = true, AllowAutoRedirect = true }) { Timeout = TimeSpan.FromSeconds(25) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (NotchIsland)");
        var t = new System.Timers.Timer(1800_000); t.Elapsed += async (_, _) => await RefreshAsync(); t.Start();
        var s = new System.Timers.Timer(1000); s.Elapsed += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateState); s.Start();
        _ = RefreshAsync();
    }

    public bool IsConfigured => Settings.Current.BakalariUser != "" && Settings.Current.BakalariClass != "" && Secure.Unprotect(Settings.Current.BakalariPassword) != "";

    async Task LoginAsync()
    {
        var s = Settings.Current; var baseUri = new Uri(s.BakalariServer.TrimEnd('/') + "/");
        try { await http.GetAsync(new Uri(baseUri, "login")); } catch { }
        var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["username"] = s.BakalariUser, ["password"] = Secure.Unprotect(s.BakalariPassword), ["persistent"] = "true", ["returnUrl"] = "/Timetable/Public" });
        var r = await http.PostAsync(new Uri(baseUri, "Login"), form);
        var html = await r.Content.ReadAsStringAsync();
        if (html.Contains("id=\"formlogin\"") || html.Contains("name=\"password\"")) throw new Exception("Login failed (username/password)");
        loggedIn = true;
    }
    async Task<string> FetchAsync(string kind)
    {
        var s = Settings.Current; var baseUri = new Uri(s.BakalariServer.TrimEnd('/') + "/");
        return await http.GetStringAsync(new Uri(baseUri, $"Timetable/Public/{kind}/Class/{Uri.EscapeDataString(s.BakalariClass)}"));
    }

    public async Task LoadClassesAsync()
    {
        Loading = true;
        try
        {
            await LoginAsync();
            var html = await http.GetStringAsync(new Uri(new Uri(Settings.Current.BakalariServer.TrimEnd('/') + "/"), "Timetable/Public"));
            var m = Regex.Match(html, "<select[^>]*id=\"selectedClass\"[^>]*>(.*?)</select>", RegexOptions.Singleline);
            var list = Regex.Matches(m.Groups[1].Value, "<option[^>]*value=\"([^\"]*)\"[^>]*>([^<]*)<").Select(x => (x.Groups[1].Value, x.Groups[2].Value.Trim())).Where(x => x.Item1 != "").ToList();
            Classes = list; Status = list.Count == 0 ? "No classes found" : "";
            if (Settings.Current.BakalariClass == "" && list.Count > 0) { Settings.Current.BakalariClass = list[0].Item1; Settings.Current.Save(); }
            await RefreshAsync(true);
        }
        catch (Exception e) { Status = e.Message; loggedIn = false; }
        finally { Loading = false; }
    }

    public async Task RefreshAsync(bool force = false)
    {
        if (!IsConfigured) { Status = L.T("Fill in Bakaláři in Settings"); return; }
        if (!force && Actual != null && (DateTime.Now - Actual.FetchedAt).TotalMinutes < 10) return;
        Loading = true;
        try
        {
            if (!loggedIn) await LoginAsync();
            var html = await FetchAsync("Actual");
            if (html.Contains("id=\"formlogin\"")) { await LoginAsync(); html = await FetchAsync("Actual"); }
            var t = Parse(html); Actual = t; Status = t.Lessons.Count == 0 ? "Timetable is empty (check the class)" : "";
            try { var n = Parse(await FetchAsync("Next")); if (n.WeekStart == t.WeekStart) n = n with { WeekStart = t.WeekStart.AddDays(7) }; Next = n; } catch { }
            try { Permanent = Parse(await FetchAsync("Permanent")); } catch { }
            UpdateState();
        }
        catch (Exception e) { Status = e.Message; loggedIn = false; Log.W("bakalari: " + e); }
        finally { Loading = false; }
    }

    /// `const timetableData = {...}` → rozvrh. Čísla hodin podle horního seznamu Hours (interní Index je +2).
    static Timetable Parse(string html)
    {
        var i = html.IndexOf("const timetableData = "); if (i < 0) return new Timetable(DateTime.Today, new(), new(), DateTime.Now);
        i += "const timetableData = ".Length; int depth = 0, end = -1; bool inStr = false, esc = false;
        for (int k = i; k < html.Length; k++) { var c = html[k]; if (inStr) { if (esc) esc = false; else if (c == '\\') esc = true; else if (c == '"') inStr = false; } else if (c == '"') inStr = true; else if (c == '{') depth++; else if (c == '}') { depth--; if (depth == 0) { end = k + 1; break; } } }
        var root = JsonDocument.Parse(html[i..end]).RootElement;
        var hours = new Dictionary<int, HourRef>(); var capByBegin = new Dictionary<string, int>();
        int idx = 0;
        foreach (var h in root.GetProperty("Hours").EnumerateArray()) { var cap = int.TryParse(h.GetProperty("Caption").GetString(), out var cc) ? cc : idx; var b = h.GetProperty("BeginTime").GetString() ?? ""; hours[cap] = new HourRef(cap, b, h.GetProperty("EndTime").GetString() ?? ""); capByBegin[b] = cap; idx++; }
        static string Hm(string? s) { if (s == null) return ""; var p = s.Split(':'); return p.Length >= 2 ? $"{int.Parse(p[0])}:{p[1]}" : s; }
        var lessons = new List<Lesson>(); DateTime? week = null; int day = 0;
        foreach (var d in root.GetProperty("Days").EnumerateArray())
        {
            if (day == 0 && d.TryGetProperty("Date", out var ds)) { var t = (ds.GetString() ?? "").Replace(" ", ""); if (t.EndsWith(".")) t += DateTime.Now.Year; if (DateTime.TryParseExact(t, "d.M.yyyy", null, System.Globalization.DateTimeStyles.None, out var wd)) week = wd; }
            foreach (var hour in d.GetProperty("Hours").EnumerateArray())
            {
                var begin = Hm(hour.TryGetProperty("Begin", out var bg) ? bg.GetString() : null);
                var raw = hour.GetProperty("Index").GetInt32();
                var id = capByBegin.TryGetValue(begin, out var cap2) ? cap2 : raw - 2;
                var atoms = hour.TryGetProperty("Atoms", out var at) ? at.EnumerateArray().ToList() : new();
                if (atoms.Count == 0 && hour.TryGetProperty("InfoRemoved", out var ir) && ir.ValueKind == JsonValueKind.String && ir.GetString() != "") { lessons.Add(new Lesson(day, id, "—", ir.GetString()!, null, null, null, null, null, null, null, "removed", ir.GetString())); continue; }
                foreach (var a in atoms)
                {
                    string S(string k) => a.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
                    var change = S("ChangeInfo"); var type = S("Type").ToLowerInvariant();
                    if (type == "removed") { var m = Regex.Match(change, @"\(([^,)]+)(?:,\s*([^)]+))?\)"); var subj = m.Success ? m.Groups[1].Value.Trim() : "—"; lessons.Add(new Lesson(day, id, subj, subj, m.Groups[2].Value, m.Groups[2].Value, null, null, null, null, null, "removed", change == "" ? "Zrušeno" : change)); continue; }
                    var hasChanged = (a.TryGetProperty("HasChanged", out var hc) && hc.GetBoolean()) || (change != "" && !(a.TryGetProperty("InfoChangeCode", out var ic) && ic.ValueKind == JsonValueKind.Number && ic.GetInt32() == 2));
                    var state = a.TryGetProperty("HasAbsent", out var ha) && ha.GetBoolean() ? "absent" : a.TryGetProperty("NewAtom", out var na) && na.GetBoolean() ? "added" : hasChanged ? "changed" : "normal";
                    var grp = S("GroupsNames"); var gm = Regex.Match(grp, @"(\d+)"); var group = grp == "" || grp.ToLowerInvariant().Contains("celá") ? null : gm.Success ? gm.Groups[1].Value + ".sk" : grp;
                    lessons.Add(new Lesson(day, id, S("SubjectAbbrev") == "" ? S("SubjectText") : S("SubjectAbbrev"), S("SubjectText"), S("Teacher"), S("TeacherFullname"), S("Room"), S("RoomFullName"), group, S("Theme"), S("Notice"), state, change == "" ? null : change));
                }
            }
            day++;
        }
        var monday = week ?? DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        return new Timetable(monday.Date, hours.Values.OrderBy(h => h.Id).ToList(), lessons, DateTime.Now);
    }

    public List<HourRef> AllHours { get { var known = (Actual?.Hours ?? new()).ToDictionary(h => h.Id); return Enumerable.Range(0, 13).Select(i => known.TryGetValue(i, out var h) ? h : new HourRef(i, SchoolHours[i].Item1, SchoolHours[i].Item2)).ToList(); } }
    static DateTime WeekOf(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));
    public Timetable? For(DateTime day) { if (Actual != null && WeekOf(day) == Actual.WeekStart) return Actual; if (Next != null && WeekOf(day) == Next.WeekStart) return Next; return Permanent ?? Actual; }
    public string Source(DateTime day) { if (Actual != null && WeekOf(day) == Actual.WeekStart) return ""; if (Next != null && WeekOf(day) == Next.WeekStart) return "next week"; return Permanent != null ? "permanent" : ""; }

    public List<TodayLesson> Lessons(DateTime day)
    {
        var t = For(day); if (t == null) return new();
        var wd = ((int)day.DayOfWeek + 6) % 7; if (wd > 4) return new();
        var g = Settings.Current.BakalariGroup; var all = AllHours;
        var list = new List<TodayLesson>();
        foreach (var h in all)
            foreach (var l in t.Lessons.Where(l => l.Day == wd && l.Hour == h.Id))
            {
                if (g != 0 && l.Group != null) { var m = Regex.Match(l.Group, @"\d"); if (m.Success && int.Parse(m.Value) != g) continue; }
                if (!TimeSpan.TryParse(h.Begin.Length == 4 ? "0" + h.Begin : h.Begin, out var b) || !TimeSpan.TryParse(h.End.Length == 4 ? "0" + h.End : h.End, out var e)) continue;
                list.Add(new TodayLesson(h, l, day.Date + b, day.Date + e));
            }
        return list;
    }

    void UpdateState()
    {
        if (!IsConfigured) { StateText = ""; WingText = ""; IsBreak = false; return; }
        var list = Lessons(DateTime.Today).Where(l => !l.Lesson.IsCancelled).ToList(); var now = DateTime.Now;
        string txt = "", wing = ""; bool brk = false; string key = "";
        static string M(TimeSpan t) => $"{Math.Max(0, (int)Math.Ceiling(t.TotalMinutes))} min";
        static string Mm(TimeSpan t) { var s = Math.Max(0, (int)t.TotalSeconds); return $"{s / 60}:{s % 60:00}"; }
        if (list.Count == 0) { }
        else if (list.FirstOrDefault(l => l.Start <= now && l.End > now) is { } cur) { txt = $"{L.T("Now")}: {cur.Lesson.Abbrev} ({cur.Lesson.Room}) · {L.T("ends in")} {M(cur.End - now)}"; if ((cur.End - now).TotalMinutes < 5) wing = Mm(cur.End - now); key = "lesson-" + cur.Hour.Id; }
        else if (list.FirstOrDefault(l => l.Start > now) is { } nx)
        {
            var prev = list.LastOrDefault(l => l.End <= now);
            if (prev != null) { var len = (nx.Start - prev.End).TotalMinutes; txt = $"{L.T("Break")} {(int)len} min · {L.T("next")} {nx.Lesson.Abbrev} ({nx.Lesson.Room}) {L.T("in")} {M(nx.Start - now)}"; wing = $"{Mm(nx.Start - now)} → {nx.Lesson.Abbrev}"; brk = true; key = "break-" + nx.Hour.Id; if (key != notified) Notify?.Invoke($"{L.T("Break")} {(int)len} min", $"{L.T("next")}: {nx.Lesson.Name} {nx.Hour.Begin}, {nx.Lesson.Room}"); }
            else txt = $"{L.T("First lesson")} {nx.Lesson.Abbrev} {L.T("in")} {M(nx.Start - now)}";
        }
        else { txt = L.T("School's out"); key = "done-" + DateTime.Today.DayOfYear; }
        if (key != "") notified = key;
        if (StateText != txt) StateText = txt; if (WingText != wing) WingText = wing; if (IsBreak != brk) IsBreak = brk;
    }
}
