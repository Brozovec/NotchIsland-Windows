using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

/// Rychlé poznámky – jeden soubor, ukládá se půl sekundy po změně.
public sealed partial class NotesService : ObservableObject
{
    public static NotesService Instance { get; } = new();
    static string File => Path.Combine(Settings.Dir, "notes.txt");
    [ObservableProperty] string text = "";
    System.Timers.Timer? save;
    NotesService() { try { if (System.IO.File.Exists(File)) text = System.IO.File.ReadAllText(File); } catch { } }
    partial void OnTextChanged(string value)
    {
        save?.Stop(); save = new System.Timers.Timer(500) { AutoReset = false };
        save.Elapsed += (_, _) => { try { Directory.CreateDirectory(Settings.Dir); System.IO.File.WriteAllText(File, value); } catch { } };
        save.Start();
    }
}
