using System.Net.Http.Json;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

/// Open-Meteo podle polohy z IP adresy (bez klíče).
public sealed partial class WeatherService : ObservableObject
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    public static WeatherService Instance { get; } = new();
    public record Hour(DateTime Time, double Temp, int Code, bool IsDay);
    [ObservableProperty] double temp;
    [ObservableProperty] double tMax;
    [ObservableProperty] double tMin;
    [ObservableProperty] int code;
    [ObservableProperty] bool isDay = true;
    [ObservableProperty] string city = "";
    [ObservableProperty] bool loaded;
    [ObservableProperty] List<Hour> hours = new();
    double lat, lon;

    WeatherService()
    {
        var t = new System.Timers.Timer(600_000); t.Elapsed += async (_, _) => await RefreshAsync(); t.Start();
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            if (lat == 0 && lon == 0)
            {
                var ip = await Http.GetFromJsonAsync<JsonElement>("https://ipapi.co/json/");
                lat = ip.GetProperty("latitude").GetDouble(); lon = ip.GetProperty("longitude").GetDouble();
                var c = ip.TryGetProperty("city", out var cc) ? cc.GetString() ?? "" : "";
                Avalonia.Threading.Dispatcher.UIThread.Post(() => City = c);
            }
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current=temperature_2m,weather_code,is_day&hourly=temperature_2m,weather_code,is_day&daily=temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=2";
            var j = await Http.GetFromJsonAsync<JsonElement>(url);
            var cur = j.GetProperty("current"); var daily = j.GetProperty("daily"); var hourly = j.GetProperty("hourly");
            var hs = new List<Hour>();
            var times = hourly.GetProperty("time").EnumerateArray().Select(x => DateTime.Parse(x.GetString()!)).ToList();
            var temps = hourly.GetProperty("temperature_2m").EnumerateArray().Select(x => x.GetDouble()).ToList();
            var codes = hourly.GetProperty("weather_code").EnumerateArray().Select(x => x.GetInt32()).ToList();
            var days = hourly.GetProperty("is_day").EnumerateArray().Select(x => x.GetInt32() == 1).ToList();
            for (int i = 0; i < times.Count && hs.Count < 6; i++) if (times[i] >= DateTime.Now.AddHours(-1)) hs.Add(new Hour(times[i], temps[i], codes[i], days[i]));
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Temp = cur.GetProperty("temperature_2m").GetDouble(); Code = cur.GetProperty("weather_code").GetInt32(); IsDay = cur.GetProperty("is_day").GetInt32() == 1;
                TMax = daily.GetProperty("temperature_2m_max")[0].GetDouble(); TMin = daily.GetProperty("temperature_2m_min")[0].GetDouble();
                Hours = hs; Loaded = true;
            });
        }
        catch (Exception e) { Log.W("weather: " + e.Message); }
    }

    public static string Text(int code) => code switch
    {
        0 => L.T("Clear"), 1 => L.T("Mostly clear"), 2 => L.T("Partly cloudy"), 3 => L.T("Overcast"), 45 or 48 => L.T("Fog"),
        >= 51 and <= 57 => L.T("Drizzle"), >= 61 and <= 67 => L.T("Rain"), >= 71 and <= 77 or 85 or 86 => L.T("Snow"), >= 80 and <= 82 => L.T("Showers"), >= 95 => L.T("Thunderstorm"), _ => "–"
    };
    /// Emoji ikona (Segoe UI Emoji na Windows)
    public static string Icon(int code, bool day) => code switch
    {
        0 => day ? "☀️" : "🌙", 1 or 2 => day ? "🌤️" : "☁️", 3 => "☁️", 45 or 48 => "🌫️", >= 51 and <= 67 or >= 80 and <= 82 => "🌧️", >= 71 and <= 77 or 85 or 86 => "🌨️", >= 95 => "⛈️", _ => "☁️"
    };
    public static (string, string) Gradient(int code, bool day)
    {
        if (!day) return ("#1B2440", "#0B0F1F");
        return code switch { 0 => ("#3A8DE0", "#7CC0F5"), 1 or 2 => ("#4A93D6", "#9CC6E8"), 3 or 45 or 48 => ("#6E7C8C", "#A3ADB8"), >= 51 and <= 67 or >= 80 and <= 82 => ("#4A5A6E", "#7A8CA0"), >= 71 and <= 86 => ("#8FA6BD", "#D6E1EA"), >= 95 => ("#2C3444", "#55637A"), _ => ("#4A93D6", "#9CC6E8") };
    }
}
