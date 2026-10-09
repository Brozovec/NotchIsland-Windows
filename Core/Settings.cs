using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotchIsland.Core;

/// Uživatelské nastavení – JSON v %APPDATA%\NotchIsland\settings.json (na macOS ~/Library/Application Support).
public sealed class Settings
{
    public static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotchIsland");
    static string File => Path.Combine(Dir, "settings.json");
    public static Settings Current { get; } = Load();

    public string GolemioToken { get; set; } = "";
    public List<string> FavoriteStops { get; set; } = new() { "Anděl" };
    public List<string> FavoriteRoutes { get; set; } = new() { "Praha|Brno" };
    public string BakalariServer { get; set; } = "https://mot-spsd.bakalari.cz";
    public string BakalariUser { get; set; } = "";
    public string BakalariPassword { get; set; } = "";   // uloženo lokálně; na Windows chráněno DPAPI (viz Secure)
    public string BakalariClass { get; set; } = "";
    public int BakalariGroup { get; set; } = 0;
    public string ScreenshotFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "NotchIsland");
    public List<string> DisabledTabs { get; set; } = new();
    public bool LaunchAtLogin { get; set; } = true;
    public string Language { get; set; } = "";   // "" = podle systému
    public int ClipboardLimit { get; set; } = 10;

    static Settings Load()
    {
        try { if (System.IO.File.Exists(File)) return JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllText(File)) ?? new Settings(); }
        catch { }
        return new Settings();
    }
    public void Save()
    {
        Directory.CreateDirectory(Dir);
        System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}

/// Jednoduché logování do %APPDATA%\NotchIsland\log.txt
public static class Log
{
    static readonly object Lock = new();
    public static void W(string s)
    {
        try { lock (Lock) { Directory.CreateDirectory(Settings.Dir); System.IO.File.AppendAllText(Path.Combine(Settings.Dir, "log.txt"), $"{DateTime.Now:HH:mm:ss.fff} {s}\n"); } } catch { }
    }
}
