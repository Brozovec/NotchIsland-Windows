using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

public record Departure(string Line, int RouteType, string Headsign, DateTime Scheduled, DateTime Predicted, int DelayMin, string Platform, string TripId)
{
    public int Minutes => Math.Max(0, (int)(Predicted - DateTime.Now).TotalMinutes);
    public string Color => RouteType switch { 0 => "#9A2B2B", 1 => "#1F6E43", 2 => "#1F4E9A", 3 => "#1D5FB8", 11 => "#6A1B9A", _ => "#555555" };
    public string Icon => RouteType switch { 0 => "🚋", 1 => "Ⓜ", 2 => "🚆", 3 or 11 => "🚌", _ => "🚏" };
    public string DelayText => DelayMin > 0 ? $"+{DelayMin}′" : L.T("on time");
    public string MinutesText => Minutes == 0 ? L.T("now") : $"{Minutes} {L.T("min")}";
    public string Sub => Scheduled.ToString("H:mm") + (Platform != "" ? "  " + Platform : "");
}

public record Connection(string Id, string Carrier, string From, string To, DateTime Departure, DateTime Arrival, bool IsTrain, int Transfers, double? Price, int? FreeSeats, int? DelayMin, string? BuyUrl)
{
    public string Times => $"{Departure:H:mm} → {Arrival:H:mm}";
    public string Dur { get { var m = (int)(Arrival - Departure).TotalMinutes; return $"{m / 60}:{m % 60:00} h"; } }
    public string CarrierColor => Carrier == "RegioJet" ? "#FFD200" : "#73D700";
    public string CarrierFg => Carrier == "RegioJet" ? "#000000" : "#FFFFFF";
    public string Extra => (FreeSeats is int f ? (f == 0 ? L.T("sold out") : $"{f} {L.T("seats")}") + "  " : "") + (Price is double p ? $"{(int)p} Kč" : "");
    public string Live => Departure <= DateTime.Now && Arrival >= DateTime.Now ? (DelayMin > 0 ? $"+{DelayMin} min" : "● " + L.T("on time")) : "";
}

/// Odjezdy PID (Golemio, vyžaduje token) + spojení RegioJet / FlixBus (bez klíče).
public sealed partial class TransitService : ObservableObject
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
    public static TransitService Instance { get; } = new();
    [ObservableProperty] string stopQuery = Settings.Current.FavoriteStops.FirstOrDefault() ?? "";
    [ObservableProperty] List<Departure> departures = new();
    [ObservableProperty] string stopStatus = "";
    [ObservableProperty] string from = "Praha";
    [ObservableProperty] string to = "Brno";
    [ObservableProperty] List<Connection> connections = new();
    [ObservableProperty] string routeStatus = "";
    [ObservableProperty] bool loading;
    [ObservableProperty] List<string> suggestions = new();
    List<(int id, string name, Dictionary<int, string> stations)> rjCities = new();
    (int, int)? rjIds; (string, string)? fbIds;

    TransitService() { Http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 NotchIsland"); }

    // ---- PID
    public async Task RefreshStopAsync()
    {
        var q = StopQuery.Trim(); if (q == "") { Departures = new(); return; }
        var token = Settings.Current.GolemioToken.Trim();
        if (token == "") { StopStatus = L.T("Enter a Golemio token in Settings (free at api.golemio.cz)"); return; }
        Loading = true;
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.golemio.cz/v2/pid/departureboards?names[]={HttpUtility.UrlEncode(q)}&minutesAfter=120&limit=40&order=real");
            req.Headers.Add("X-Access-Token", token);
            var r = await Http.SendAsync(req);
            if (r.StatusCode == System.Net.HttpStatusCode.NotFound) { Departures = new(); StopStatus = L.T("Unknown stop"); return; }
            r.EnsureSuccessStatusCode();
            var j = await r.Content.ReadFromJsonAsync<JsonElement>();
            var list = new List<Departure>();
            foreach (var d in j.GetProperty("departures").EnumerateArray())
            {
                var route = d.GetProperty("route"); var trip = d.GetProperty("trip"); var dep = d.GetProperty("departure_timestamp"); var delay = d.GetProperty("delay"); var st = d.GetProperty("stop");
                var sched = DateTime.Parse(dep.GetProperty("scheduled").GetString()!).ToLocalTime();
                var pred = dep.TryGetProperty("predicted", out var pp) && pp.ValueKind == JsonValueKind.String ? DateTime.Parse(pp.GetString()!).ToLocalTime() : sched;
                var mins = delay.TryGetProperty("is_available", out var av) && av.GetBoolean() && delay.TryGetProperty("minutes", out var mm) ? mm.GetInt32() : 0;
                list.Add(new Departure(route.GetProperty("short_name").GetString() ?? "?", route.GetProperty("type").GetInt32(), trip.TryGetProperty("headsign", out var hs) ? hs.GetString() ?? "" : "",
                    sched, pred, mins, st.TryGetProperty("platform_code", out var pc) && pc.ValueKind == JsonValueKind.String ? pc.GetString()! : "", trip.GetProperty("id").GetString() ?? ""));
            }
            Departures = list; StopStatus = list.Count == 0 ? L.T("No departures within 2 h") : "";
        }
        catch (Exception e) { StopStatus = "Golemio: " + e.Message; Log.W("transit: " + e); }
        finally { Loading = false; }
    }

    // ---- RegioJet + FlixBus
    public async Task SearchAsync()
    {
        var f = From.Trim(); var t = To.Trim(); if (f == "" || t == "") return;
        Loading = true; RouteStatus = "";
        try
        {
            var rj = SearchRegioJetAsync(f, t); var fb = SearchFlixBusAsync(f, t);
            var all = (await rj).Concat(await fb).Where(c => c.Departure > DateTime.Now.AddMinutes(-30)).OrderBy(c => c.Departure).ToList();
            Connections = all;
            if (all.Count == 0) RouteStatus = L.T("Enter from and to, searches today's connections");
        }
        catch (Exception e) { RouteStatus = e.Message; }
        finally { Loading = false; }
    }

    async Task LoadRjCitiesAsync()
    {
        if (rjCities.Count > 0) return;
        try
        {
            var j = await Http.GetFromJsonAsync<JsonElement>("https://brn-ybus-pubapi.sa.cz/restapi/consts/locations");
            var list = new List<(int, string, Dictionary<int, string>)>();
            foreach (var country in j.EnumerateArray())
                foreach (var c in country.GetProperty("cities").EnumerateArray())
                {
                    var st = new Dictionary<int, string>();
                    foreach (var s in c.GetProperty("stations").EnumerateArray()) st[s.GetProperty("id").GetInt32()] = s.TryGetProperty("fullname", out var fn) ? fn.GetString() ?? "" : s.GetProperty("name").GetString() ?? "";
                    list.Add((c.GetProperty("id").GetInt32(), c.GetProperty("name").GetString() ?? "", st));
                }
            rjCities = list;
        }
        catch (Exception e) { Log.W("rj cities: " + e.Message); }
    }
    static string Norm(string s) => string.Concat(s.Normalize(System.Text.NormalizationForm.FormD).Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)).ToLowerInvariant().Trim();

    public async Task SuggestAsync(string q)
    {
        var n = Norm(q); if (n.Length < 2) { Suggestions = new(); return; }
        await LoadRjCitiesAsync();
        var list = rjCities.Select(c => c.name).Where(c => Norm(c).StartsWith(n)).Take(6).ToList();
        if (list.Count < 4) try { var j = await Http.GetFromJsonAsync<JsonElement>($"https://global.api.flixbus.com/search/autocomplete/cities?q={HttpUtility.UrlEncode(q)}&lang=cs&country=cz"); foreach (var c in j.EnumerateArray()) { var nm = c.GetProperty("name").GetString() ?? ""; if (!list.Contains(nm)) list.Add(nm); } } catch { }
        Suggestions = list.Take(6).ToList();
    }

    async Task<List<Connection>> SearchRegioJetAsync(string from, string to)
    {
        await LoadRjCitiesAsync();
        var a = rjCities.FirstOrDefault(c => Norm(c.name) == Norm(from)); if (a.name == null) a = rjCities.FirstOrDefault(c => Norm(c.name).StartsWith(Norm(from)));
        var b = rjCities.FirstOrDefault(c => Norm(c.name) == Norm(to)); if (b.name == null) b = rjCities.FirstOrDefault(c => Norm(c.name).StartsWith(Norm(to)));
        if (a.name == null || b.name == null) return new();
        rjIds = (a.id, b.id);
        var stations = rjCities.SelectMany(c => c.stations).GroupBy(k => k.Key).ToDictionary(g => g.Key, g => g.First().Value);
        var req = new HttpRequestMessage(HttpMethod.Get, $"https://brn-ybus-pubapi.sa.cz/restapi/routes/search/simple?fromLocationId={a.id}&fromLocationType=CITY&toLocationId={b.id}&toLocationType=CITY&departureDate={DateTime.Today:yyyy-MM-dd}&tariffs=REGULAR");
        req.Headers.Add("X-Currency", "CZK"); req.Headers.Add("X-Lang", "cs");
        var j = await (await Http.SendAsync(req)).Content.ReadFromJsonAsync<JsonElement>();
        var list = new List<Connection>();
        if (!j.TryGetProperty("routes", out var routes)) return list;
        foreach (var r in routes.EnumerateArray())
        {
            var types = r.GetProperty("vehicleTypes").EnumerateArray().Select(x => x.GetString()).ToList();
            int? delay = null; if (r.TryGetProperty("delay", out var dl)) { if (dl.ValueKind == JsonValueKind.String && int.TryParse(new string(dl.GetString()!.Where(char.IsDigit).ToArray()), out var dm)) delay = dm; else if (dl.ValueKind == JsonValueKind.Number) delay = dl.GetInt32(); }
            var price = r.TryGetProperty("priceFrom", out var pf) && pf.ValueKind == JsonValueKind.Number && pf.GetDouble() > 0 ? pf.GetDouble() : (double?)null;
            var dep = DateTime.Parse(r.GetProperty("departureTime").GetString()!); var arr = DateTime.Parse(r.GetProperty("arrivalTime").GetString()!);
            list.Add(new Connection("rj-" + r.GetProperty("id").GetString(), "RegioJet", stations.GetValueOrDefault(r.GetProperty("departureStationId").GetInt32(), a.name), stations.GetValueOrDefault(r.GetProperty("arrivalStationId").GetInt32(), b.name),
                dep, arr, types.Contains("TRAIN") && !types.Contains("BUS"), r.TryGetProperty("transfersCount", out var tc) ? tc.GetInt32() : 0, price, r.TryGetProperty("freeSeatsCount", out var fs) ? fs.GetInt32() : null, delay,
                $"https://regiojet.cz/vyhledavani?fromLocationId={a.id}&fromLocationType=CITY&toLocationId={b.id}&toLocationType=CITY&departureDate={dep:yyyy-MM-dd}&tariffs=REGULAR"));
        }
        return list;
    }

    async Task<(string id, string name)?> FbCityAsync(string q)
    {
        try { var j = await Http.GetFromJsonAsync<JsonElement>($"https://global.api.flixbus.com/search/autocomplete/cities?q={HttpUtility.UrlEncode(q)}&lang=cs&country=cz"); var c = j.EnumerateArray().FirstOrDefault(); if (c.ValueKind == JsonValueKind.Object) return (c.GetProperty("id").GetString()!, c.GetProperty("name").GetString() ?? q); } catch { }
        return null;
    }
    async Task<List<Connection>> SearchFlixBusAsync(string from, string to)
    {
        var a = await FbCityAsync(from); var b = await FbCityAsync(to); if (a == null || b == null) return new();
        fbIds = (a.Value.id, b.Value.id);
        var url = $"https://global.api.flixbus.com/search/service/v4/search?from_city_id={a.Value.id}&to_city_id={b.Value.id}&departure_date={DateTime.Today:dd.MM.yyyy}&products=%7B%22adult%22%3A1%7D&currency=CZK&locale=cs&search_by=cities&include_after_midnight_rides=1";
        var j = await Http.GetFromJsonAsync<JsonElement>(url);
        var list = new List<Connection>();
        if (!j.TryGetProperty("trips", out var trips) || trips.GetArrayLength() == 0) return list;
        var stations = j.TryGetProperty("stations", out var sts) ? sts : default;
        foreach (var kv in trips[0].GetProperty("results").EnumerateObject())
        {
            var r = kv.Value;
            var dep = DateTime.Parse(r.GetProperty("departure").GetProperty("date").GetString()!); var arr = DateTime.Parse(r.GetProperty("arrival").GetProperty("date").GetString()!);
            string Name(JsonElement side) { var sid = side.GetProperty("station_id").GetString() ?? ""; return stations.ValueKind == JsonValueKind.Object && stations.TryGetProperty(sid, out var s) ? s.GetProperty("name").GetString() ?? "" : ""; }
            var legs = r.TryGetProperty("legs", out var lg) ? lg.GetArrayLength() : 1;
            var sold = r.TryGetProperty("status", out var stt) && stt.GetString() != "available";
            list.Add(new Connection("fb-" + kv.Name, "FlixBus", Name(r.GetProperty("departure")) is var dn && dn != "" ? dn : a.Value.name, Name(r.GetProperty("arrival")) is var an && an != "" ? an : b.Value.name,
                dep, arr, false, Math.Max(0, legs - 1), r.TryGetProperty("price", out var pr) && pr.TryGetProperty("total", out var tot) ? tot.GetDouble() : null, sold ? 0 : null, null,
                $"https://shop.flixbus.cz/search?departureCity={a.Value.id}&arrivalCity={b.Value.id}&rideDate={dep:dd.MM.yyyy}&adult=1"));
        }
        return list;
    }
    public string IdosUrl() => $"https://idos.cz/vlakyautobusymhdvse/spojeni/vysledky/?f={HttpUtility.UrlEncode(From)}&t={HttpUtility.UrlEncode(To)}";
}
