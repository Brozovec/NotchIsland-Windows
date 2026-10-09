using System.Globalization;

namespace NotchIsland.Core;

/// Lokalizace: klíč = angličtina, překlady do češtiny (ostatní jazyky lze doplnit do slovníku).
public static class L
{
    public static string Lang = string.IsNullOrEmpty(Settings.Current.Language) ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName : Settings.Current.Language;
    static readonly Dictionary<string, string> Cs = new()
    {
        ["Island"] = "Island", ["Tray"] = "Tray", ["Transit"] = "Doprava", ["Calls"] = "Hovory", ["Shot"] = "Shot", ["Notes"] = "Poznámky", ["Clipboard"] = "Schránka", ["Timer"] = "Časovač", ["Timetable"] = "Rozvrh", ["Settings"] = "Nastavení",
        ["Nothing playing"] = "Nic nehraje", ["Nothing today"] = "Nic na dnešek", ["Drop files here"] = "Přetáhni sem soubory",
        ["Stop"] = "Zastávka", ["Routes"] = "Spojení", ["From"] = "Odkud", ["To"] = "Kam", ["on time"] = "včas", ["now"] = "teď", ["min"] = "min", ["seats"] = "míst", ["sold out"] = "vyprodáno",
        ["Enter a stop"] = "Zadej zastávku", ["Unknown stop"] = "Zastávku neznám", ["No departures within 2 h"] = "Žádné odjezdy do 2 h", ["Enter from and to, searches today's connections"] = "Zadej odkud a kam, hledá dnešní spoje",
        ["Golemio token"] = "Golemio token", ["Enter a Golemio token in Settings (free at api.golemio.cz)"] = "Zadej Golemio token v Nastavení (zdarma na api.golemio.cz)",
        ["Today"] = "Dnes", ["Week"] = "Týden", ["Cancelled"] = "Zrušeno", ["No lessons today"] = "Dnes žádné hodiny", ["Fill in Bakaláři in Settings"] = "Vyplň Bakaláře v Nastavení", ["Break"] = "Přestávka", ["next"] = "další", ["in"] = "za", ["Now"] = "Nyní", ["ends in"] = "konec za", ["School's out"] = "Konec vyučování", ["First lesson"] = "První hodina",
        ["Sign in and load classes"] = "Přihlásit a načíst třídy", ["Server"] = "Server", ["Username"] = "Jméno", ["Password"] = "Heslo", ["Class"] = "Třída", ["all"] = "vše",
        ["Area"] = "Oblast", ["Screen"] = "Obrazovka", ["Text (OCR)"] = "Text (OCR)", ["Copied to clipboard"] = "Zkopírováno do schránky", ["No screenshots yet"] = "Zatím žádné snímky", ["Text copied"] = "Text zkopírován",
        ["Search clipboard…"] = "Hledat ve schránce…", ["Clipboard history"] = "Historie schránky", ["Anything you copy shows up here. Click = paste."] = "Cokoli zkopíruješ, objeví se tady. Klik = vložit.",
        ["Quick note… saves automatically"] = "Rychlá poznámka… ukládá se sama", ["Pomodoro"] = "Pomodoro", ["work"] = "práce", ["break"] = "pauza", ["Timer finished"] = "Časovač doběhl",
        ["Launch at login"] = "Spouštět po přihlášení", ["Modules"] = "Moduly", ["Screenshot folder"] = "Složka screenshotů", ["General"] = "Obecné", ["Language"] = "Jazyk", ["System"] = "Systém",
        ["On a call"] = "Probíhá hovor", ["No call"] = "Žádný hovor", ["No call app running"] = "Žádná hovorová appka neběží",
        ["Clear"] = "Jasno", ["Mostly clear"] = "Skoro jasno", ["Partly cloudy"] = "Polojasno", ["Overcast"] = "Zataženo", ["Fog"] = "Mlha", ["Drizzle"] = "Mrholení", ["Rain"] = "Déšť", ["Snow"] = "Sněžení", ["Showers"] = "Přeháňky", ["Thunderstorm"] = "Bouřka", ["Locating…"] = "Zjišťuji polohu…",
        ["Quit"] = "Ukončit", ["Open"] = "Otevřít", ["Buy ticket"] = "Koupit jízdenku", ["Track"] = "Sledovat", ["Favorite"] = "Oblíbené",
    };
    public static string T(string key) => Lang == "cs" && Cs.TryGetValue(key, out var v) ? v : key;
}
