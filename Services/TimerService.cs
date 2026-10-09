using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

/// Časovač / Pomodoro (25/5, po 4 kolech 15 min). Běží i ve sbaleném ostrůvku.
public sealed partial class TimerService : ObservableObject
{
    public static TimerService Instance { get; } = new();
    [ObservableProperty] double remaining;
    [ObservableProperty] double total;
    [ObservableProperty] bool running;
    [ObservableProperty] bool isPomodoro;
    [ObservableProperty] bool isBreak;
    [ObservableProperty] int round;
    public bool IsActive => Total > 0;
    public string Text { get { var s = (int)Math.Round(Remaining); return s >= 3600 ? $"{s / 3600}:{s / 60 % 60:00}:{s % 60:00}" : $"{s / 60}:{s % 60:00}"; } }
    DateTime end;
    readonly System.Timers.Timer tick = new(250);
    public event Action<string>? Finished;

    TimerService() { tick.Elapsed += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(Update); }

    public void Start(int minutes, bool pomodoro = false)
    {
        IsPomodoro = pomodoro; IsBreak = false; Round = pomodoro ? 1 : 0;
        Total = Remaining = minutes * 60; Resume(); OnPropertyChanged(nameof(IsActive));
    }
    public void Toggle() { if (Running) Pause(); else Resume(); }
    public void Pause() { Running = false; tick.Stop(); }
    public void Resume() { if (Remaining <= 0) return; Running = true; end = DateTime.Now.AddSeconds(Remaining); tick.Start(); }
    public void Stop() { Pause(); Total = Remaining = 0; IsPomodoro = false; Round = 0; OnPropertyChanged(nameof(IsActive)); OnPropertyChanged(nameof(Text)); }
    public void AddMinute() { Remaining += 60; Total += 60; if (Running) end = DateTime.Now.AddSeconds(Remaining); OnPropertyChanged(nameof(Text)); }

    void Update()
    {
        if (!Running) return;
        Remaining = Math.Max(0, (end - DateTime.Now).TotalSeconds);
        OnPropertyChanged(nameof(Text));
        if (Remaining > 0) return;
        Pause();
        if (IsPomodoro)
        {
            if (!IsBreak) { IsBreak = true; Total = Remaining = Round % 4 == 0 ? 15 * 60 : 5 * 60; Finished?.Invoke(L.T("Pomodoro") + ": " + L.T("break")); }
            else { IsBreak = false; Round++; Total = Remaining = 25 * 60; Finished?.Invoke(L.T("Pomodoro") + ": " + L.T("work")); }
            Resume();
        }
        else { Total = 0; OnPropertyChanged(nameof(IsActive)); Finished?.Invoke(L.T("Timer finished")); }
    }
}
