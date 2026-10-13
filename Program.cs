using System.Runtime.InteropServices;
using Avalonia;
using NotchIsland.Core;

namespace NotchIsland;

class Program
{
    static Mutex? single;
    public static bool AlreadyRunning;

    [STAThread]
    public static void Main(string[] args)
    {
        // jen jedna instance: druhé spuštění ohlásí a skončí
        single = new Mutex(true, "NotchIsland.SingleInstance", out var first);
        if (!first) { AlreadyRunning = true; BuildAvaloniaApp().StartWithClassicDesktopLifetime(args); return; }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        GC.KeepAlive(single);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace()
        .With(new Win32PlatformOptions { CompositionMode = new[] { Win32CompositionMode.WinUIComposition, Win32CompositionMode.RedirectionSurface }, RenderingMode = new[] { Win32RenderingMode.AngleEgl, Win32RenderingMode.Software } });
}
