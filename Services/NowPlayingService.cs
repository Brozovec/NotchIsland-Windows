using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

/// Právě hraje: na Windows přes GlobalSystemMediaTransportControls (Spotify, prohlížeč, Hudba…). Jinde prázdné.
public sealed partial class NowPlayingService : ObservableObject
{
    public static NowPlayingService Instance { get; } = new();
    [ObservableProperty] string? title;
    [ObservableProperty] string? artist;
    [ObservableProperty] string? album;
    [ObservableProperty] string? app;
    [ObservableProperty] bool isPlaying;
    [ObservableProperty] double position;
    [ObservableProperty] double duration;
    [ObservableProperty] byte[]? artwork;
    public bool HasMedia => !string.IsNullOrEmpty(Title);

#if WINDOWS
    Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager? mgr;
    Windows.Media.Control.GlobalSystemMediaTransportControlsSession? session;
    string lastArtKey = "";
#endif
    readonly System.Timers.Timer poll = new(1000);

    NowPlayingService()
    {
        poll.Elapsed += async (_, _) => await PollAsync();
        poll.Start();
        _ = PollAsync();
    }

    async Task PollAsync()
    {
#if WINDOWS
        try
        {
            mgr ??= await Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            session = mgr.GetCurrentSession();
            if (session == null) { Clear(); return; }
            var props = await session.TryGetMediaPropertiesAsync();
            var info = session.GetPlaybackInfo();
            var tl = session.GetTimelineProperties();
            var playing = info.PlaybackStatus == Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            var pos = tl.Position.TotalSeconds + (playing ? (DateTimeOffset.Now - tl.LastUpdatedTime).TotalSeconds : 0);
            byte[]? art = null;
            var key = props.Title + "|" + props.Artist + "|" + props.AlbumTitle;
            if (key != lastArtKey && props.Thumbnail != null)
            {
                using var stream = await props.Thumbnail.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.AsStreamForRead().CopyToAsync(ms);
                art = ms.ToArray(); lastArtKey = key;
            }
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Title = props.Title; Artist = props.Artist; Album = props.AlbumTitle; App = FriendlyApp(session.SourceAppUserModelId);
                IsPlaying = playing; Position = pos; Duration = tl.EndTime.TotalSeconds;
                if (art != null) Artwork = art;
                OnPropertyChanged(nameof(HasMedia));
            });
        }
        catch (Exception e) { Log.W("nowplaying: " + e.Message); }
#else
        await Task.CompletedTask;
#endif
    }

    void Clear() => Avalonia.Threading.Dispatcher.UIThread.Post(() => { Title = null; Artist = null; Album = null; App = null; IsPlaying = false; Artwork = null; OnPropertyChanged(nameof(HasMedia)); });

    static string FriendlyApp(string aumid)
    {
        var a = aumid.ToLowerInvariant();
        if (a.Contains("spotify")) return "Spotify";
        if (a.Contains("chrome")) return "Chrome";
        if (a.Contains("msedge")) return "Edge";
        if (a.Contains("firefox")) return "Firefox";
        if (a.Contains("zunemusic") || a.Contains("media")) return "Media Player";
        if (a.Contains("itunes") || a.Contains("applemusic") || a.Contains("music")) return "Apple Music";
        return aumid.Split('!', '.').FirstOrDefault() ?? aumid;
    }

    public async void PlayPause() { 
#if WINDOWS
        try { if (session != null) await session.TryTogglePlayPauseAsync(); } catch { }
#endif
        await Task.CompletedTask; }
    public async void Next() {
#if WINDOWS
        try { if (session != null) await session.TrySkipNextAsync(); } catch { }
#endif
        await Task.CompletedTask; }
    public async void Previous() {
#if WINDOWS
        try { if (session != null) await session.TrySkipPreviousAsync(); } catch { }
#endif
        await Task.CompletedTask; }
}
