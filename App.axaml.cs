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
        TaskScheduler.UnobservedTaskException += (_, e) => { Log.W("UNOBSERVED: " + e.Exception); e.SetObserved(); };
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) => { Log.W("UI: " + e.Exception); e.Handled = true; };
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d2 && Program.AlreadyRunning)
        {
            d2.ShutdownMode = ShutdownMode.OnLastWindowClose; d2.MainWindow = new AlreadyRunningWindow();
        }
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Island = new IslandWindow();
            Island.Show();
            // služby
            _ = NowPlayingService.Instance; _ = TimerService.Instance; _ = WeatherService.Instance; _ = TransitService.Instance;
            _ = BakalariService.Instance; _ = ClipboardService.Instance; _ = CallsService.Instance; _ = NotesService.Instance; _ = CalendarService.Instance;
            Platform.SetLaunchAtLogin(Settings.Current.LaunchAtLogin);
            if (!Settings.Current.FirstRunDone) { Settings.Current.LaunchAtLogin = true; Settings.Current.FirstRunDone = true; Settings.Current.Save(); Platform.SetLaunchAtLogin(true); Avalonia.Threading.DispatcherTimer.RunOnce(() => Island.Toast(L.T("NotchIsland will start with Windows. Move the mouse to the top of the screen.")), TimeSpan.FromSeconds(2)); }
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
