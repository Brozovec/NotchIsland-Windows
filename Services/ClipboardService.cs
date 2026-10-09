using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

public record ClipItem(string Id, string Text, DateTime Date, bool Pinned);

/// Historie schránky (jako Win+V): sleduje text ve schránce, max N položek, připnuté zůstávají.
public sealed partial class ClipboardService : ObservableObject
{
    public static ClipboardService Instance { get; } = new();
    static string File => Path.Combine(Settings.Dir, "clipboard.json");
    [ObservableProperty] List<ClipItem> items = new();
    [ObservableProperty] string query = "";
    string last = ""; bool suppress;
    public Func<Task<string?>>? ReadClipboard;      // nastaví UI (Avalonia clipboard)
    public Func<string, Task>? WriteClipboard;
    public Action? PasteRequested;                   // simulace Ctrl+V (Windows)

    ClipboardService()
    {
        try { if (System.IO.File.Exists(File)) items = JsonSerializer.Deserialize<List<ClipItem>>(System.IO.File.ReadAllText(File)) ?? new(); } catch { }
        var t = new System.Timers.Timer(700); t.Elapsed += async (_, _) => await PollAsync(); t.Start();
    }
    public IEnumerable<ClipItem> Filtered => (Query.Trim() == "" ? Items : Items.Where(i => i.Text.Contains(Query, StringComparison.OrdinalIgnoreCase))).OrderByDescending(i => i.Pinned).ThenByDescending(i => i.Date);

    async Task PollAsync()
    {
        if (ReadClipboard == null) return;
        string? s;
        try { s = await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ReadClipboard()); } catch { return; }
        if (string.IsNullOrWhiteSpace(s) || s == last) return;
        last = s;
        if (suppress) { suppress = false; return; }
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var list = Items.Where(i => i.Text != s).ToList();
            list.Insert(0, new ClipItem(Guid.NewGuid().ToString(), s.Length > 20000 ? s[..20000] : s, DateTime.Now, false));
            var limit = Settings.Current.ClipboardLimit; int n = 0;
            Items = list.Where(i => i.Pinned || n++ < limit).ToList(); Save();
        });
    }
    public async Task PasteAsync(ClipItem it)
    {
        suppress = true; last = it.Text;
        if (WriteClipboard != null) await WriteClipboard(it.Text);
        Items = Items.Select(i => i.Id == it.Id ? i with { Date = DateTime.Now } : i).ToList(); Save();
        PasteRequested?.Invoke();
    }
    public void TogglePin(ClipItem it) { Items = Items.Select(i => i.Id == it.Id ? i with { Pinned = !i.Pinned } : i).ToList(); Save(); }
    public void Remove(ClipItem it) { Items = Items.Where(i => i.Id != it.Id).ToList(); Save(); }
    public void Clear() { Items = Items.Where(i => i.Pinned).ToList(); Save(); }
    void Save() { try { Directory.CreateDirectory(Settings.Dir); System.IO.File.WriteAllText(File, JsonSerializer.Serialize(Items)); } catch { } OnPropertyChanged(nameof(Filtered)); }
    partial void OnQueryChanged(string value) => OnPropertyChanged(nameof(Filtered));
}
