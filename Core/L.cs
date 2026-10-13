using System.Globalization;

namespace NotchIsland.Core;

/// Lokalizace: klíč = angličtina, překlady do češtiny (ostatní jazyky lze doplnit do slovníku).
public static class L
{
    public static string Lang = string.IsNullOrEmpty(Settings.Current.Language) ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName : Settings.Current.Language;
    static readonly Dictionary<string, string> Cs = new()
    {
        ["Island"] = "Island", ["Tray"] = "Tray", ["Transit"] = "Doprava", ["Calls"] = "Hovory", ["Shot"] = "Shot", ["Notes"] = "Poznámky", ["Clipboard"] = "Schránka", ["Timer"] = "Časovač", ["Timetable"] = "Rozvrh", ["Settings"] = "Nastavení",
        ["Nothing playing"] = "Nic nehraje", ["all day"] = "celý den", ["Add a calendar ICS link in Settings"] = "Přidej ICS odkaz na kalendář v Nastavení", ["Calendar (ICS link)"] = "Kalendář (ICS odkaz)", ["Nothing today"] = "Nic na dnešek", ["Drop files here"] = "Přetáhni sem soubory",
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
        ["Weather"] = "Počasí", ["Location"] = "Poloha", ["Find"] = "Hledat", ["Not found"] = "Nenalezeno", ["Automatic (by IP address)"] = "Automaticky (podle IP adresy)", ["Leave empty for automatic location. Desktop PCs without GPS: type your city (e.g. Praha, Brno)."] = "Prázdné = automatická poloha. Stolní PC bez GPS: napiš své město (např. Praha, Brno).",
        ["NotchIsland is already running. You can only have it open once; use the icon in the system tray."] = "NotchIsland už běží. Může být spuštěný jen jednou – použij ikonu v oznamovací oblasti.", ["NotchIsland will start with Windows. Move the mouse to the top of the screen."] = "NotchIsland se bude spouštět se systémem. Najeď myší k hornímu okraji obrazovky.",
        ["NotchIsland is already running"] = "NotchIsland už běží", ["You can only have it open once. Look for the island at the top edge of the screen or the icon in the system tray."] = "Může být spuštěný jen jednou. Najdeš ho u horního okraje obrazovky nebo jako ikonu v oznamovací oblasti.", ["OK"] = "OK",
        ["Startup and language"] = "Spouštění a jazyk", ["NotchIsland starts quietly with Windows and lives at the top edge of the screen."] = "NotchIsland se potichu spustí s Windows a bydlí u horního okraje obrazovky.", ["Takes effect after restart."] = "Projeví se po restartu.",
        ["Choose which tabs the island shows"] = "Vyber, které záložky ostrůvek ukazuje", ["Drop files to keep them at hand"] = "Odlož soubory po ruce", ["Departures, RegioJet and FlixBus"] = "Odjezdy, RegioJet a FlixBus", ["Discord, Teams, Zoom status"] = "Stav hovoru v Discordu, Teams, Zoomu", ["Screenshots and text recognition"] = "Screenshoty a rozpoznání textu", ["Quick note that saves itself"] = "Rychlá poznámka, ukládá se sama", ["History of copied text"] = "Historie zkopírovaného textu", ["Timer and Pomodoro"] = "Časovač a Pomodoro", ["Bakaláři school timetable"] = "Školní rozvrh z Bakalářů",
        ["Calendar"] = "Kalendář", ["Events from any calendar via an ICS link"] = "Události z libovolného kalendáře přes ICS odkaz", ["ICS link"] = "ICS odkaz", ["Where to show the forecast for"] = "Pro jaké místo ukazovat předpověď", ["Prague departures need a free Golemio token"] = "Pražské odjezdy potřebují token Golemio (zdarma)", ["School timetable with breaks and changes"] = "Školní rozvrh s přestávkami a změnami", ["Screenshots are copied and saved to a folder"] = "Screenshoty se kopírují a ukládají do složky", ["Shortcuts"] = "Zkratky",
        ["Quit"] = "Ukončit", ["Open"] = "Otevřít", ["Buy ticket"] = "Koupit jízdenku", ["Track"] = "Sledovat", ["Favorite"] = "Oblíbené",
    };
    public static string T(string key) => Lang == "cs" && Cs.TryGetValue(key, out var v) ? v : key;
}
