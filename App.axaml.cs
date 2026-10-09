using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NotchIsland.Core;
using NotchIsland.Services;
using NotchIsland.Views;

namespace NotchIsland;

public partial class App : Application
{
    public static IslandWindow? Island { get; private set; }
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.W("UNHANDLED: " + e.ExceptionObject);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Island = new IslandWindow();
            Island.Show();
            // služby
            _ = NowPlayingService.Instance; _ = TimerService.Instance; _ = WeatherService.Instance; _ = TransitService.Instance;
            _ = BakalariService.Instance; _ = ClipboardService.Instance; _ = CallsService.Instance; _ = NotesService.Instance; _ = CalendarService.Instance;
            Platform.SetLaunchAtLogin(Settings.Current.LaunchAtLogin);
            Platform.StartHotkeys(id => Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                switch (id)
                {
                    case 1: await ShotService.Instance.CaptureAsync("screen"); break;
                    case 2: await ShotService.Instance.CaptureAsync("area"); break;
                    case 3: await ShotService.Instance.CaptureAsync("ocr"); break;
                    case 4: Island.Open("clipboard"); break;
                }
            }));
            TimerService.Instance.Finished += m => Island.Toast(m);
            ShotService.Instance.Toast += m => Island.Toast(m);
            BakalariService.Instance.Notify += (t, b) => Island.Toast(t + " · " + b);
            // ladění: NOTCH_OPEN=home spustí rozbalené na dané záložce
            if (Environment.GetEnvironmentVariable("NOTCH_OPEN") is { Length: > 0 } tab) Avalonia.Threading.DispatcherTimer.RunOnce(() => Island.Open(tab), TimeSpan.FromMilliseconds(800));
        }
        base.OnFrameworkInitializationCompleted();
    }

    void TrayClicked(object? s, EventArgs e) => Island?.Open("home");
    void TrayOpen(object? s, EventArgs e) => Island?.Open("home");
    void TraySettings(object? s, EventArgs e) => SettingsWindow.ShowIt();
    void TrayQuit(object? s, EventArgs e) { if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d) d.Shutdown(); }
}
