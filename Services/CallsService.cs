using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NotchIsland.Services;

/// Hovory: detekce běžících meetingových aplikací podle procesu.
public sealed partial class CallsService : ObservableObject
{
    public static CallsService Instance { get; } = new();
    public record CallApp(string Process, string Name, string Color);
    static readonly CallApp[] Known = {
        new("Discord", "Discord", "#5865F2"), new("Zoom", "Zoom", "#2D8CFF"), new("ms-teams", "Teams", "#6264A7"), new("Teams", "Teams", "#6264A7"),
        new("slack", "Slack", "#4A154B"), new("Telegram", "Telegram", "#2AABEE"), new("WhatsApp", "WhatsApp", "#25D366"), new("Skype", "Skype", "#00AFF0"),
    };
    [ObservableProperty] List<CallApp> running = new();
    readonly System.Timers.Timer poll = new(3000);
    CallsService() { poll.Elapsed += (_, _) => Poll(); poll.Start(); Poll(); }
    void Poll()
    {
        try
        {
            var names = Process.GetProcesses().Select(p => { try { return p.ProcessName; } catch { return ""; } }).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var list = Known.Where(k => names.Contains(k.Process)).DistinctBy(k => k.Name).ToList();
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { if (!list.Select(x => x.Name).SequenceEqual(Running.Select(x => x.Name))) Running = list; });
        }
        catch { }
    }
}
