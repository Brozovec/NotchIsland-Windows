using System.Diagnostics;
using System.Runtime.InteropServices;
using NotchIsland.Core;

namespace NotchIsland.Services;

/// Windows-specifické věci: poloha kurzoru, globální zkratky, simulace Ctrl+V, autostart, screenshot + OCR.
/// Na jiných platformách jsou to bezpečné prázdné implementace (jen pro vývoj UI).
public static class Platform
{
    public static bool IsWindows => OperatingSystem.IsWindows();

#if WINDOWS
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mod, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] static extern IntPtr CreateWindowEx(int ex, string cls, string name, int style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
    [DllImport("user32.dll")] static extern int GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);
    [DllImport("user32.dll")] static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] static extern IntPtr DispatchMessage(ref MSG msg);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int i);
    [StructLayout(LayoutKind.Sequential)] struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public POINT pt; }
    const uint MOD_CONTROL = 2, MOD_SHIFT = 4, WM_HOTKEY = 0x0312;
#endif

    /// Poloha kurzoru v pixelech obrazovky (null mimo Windows).
    public static (double x, double y)? CursorPosition()
    {
#if WINDOWS
        if (GetCursorPos(out var p)) return (p.X, p.Y);
#endif
        return null;
    }

    /// Globální zkratky: Ctrl+Shift+1 obrazovka, Ctrl+Shift+2 oblast, Ctrl+Shift+O OCR, Ctrl+Shift+V schránka. Běží v samostatném vlákně se zprávami.
    public static void StartHotkeys(Action<int> onHotkey)
    {
#if WINDOWS
        var th = new Thread(() =>
        {
            var hwnd = CreateWindowEx(0, "STATIC", "NotchIslandHotkeys", 0, 0, 0, 0, 0, new IntPtr(-3) /*HWND_MESSAGE*/, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            var keys = new (int id, uint vk)[] { (1, 0x31), (2, 0x32), (3, 0x4F), (4, 0x56) };
            foreach (var (id, vk) in keys) if (!RegisterHotKey(hwnd, id, MOD_CONTROL | MOD_SHIFT, vk)) Log.W($"hotkey {id} failed");
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                if (msg.message == WM_HOTKEY) onHotkey((int)msg.wParam);
                TranslateMessage(ref msg); DispatchMessage(ref msg);
            }
        }) { IsBackground = true, Name = "hotkeys" };
        th.Start();
#endif
    }

    /// Simuluje Ctrl+V do aktivní aplikace.
    public static void SendPaste()
    {
#if WINDOWS
        keybd_event(0x11, 0, 0, UIntPtr.Zero); keybd_event(0x56, 0, 0, UIntPtr.Zero);
        keybd_event(0x56, 0, 2, UIntPtr.Zero); keybd_event(0x11, 0, 2, UIntPtr.Zero);
#endif
    }

    /// Autostart přes HKCU\Software\Microsoft\Windows\CurrentVersion\Run
    public static void SetLaunchAtLogin(bool on)
    {
#if WINDOWS
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (on) key?.SetValue("NotchIsland", $"\"{Environment.ProcessPath}\""); else key?.DeleteValue("NotchIsland", false);
        }
        catch (Exception e) { Log.W("autostart: " + e.Message); }
#endif
    }

    /// Screenshot celé primární obrazovky jako PNG; `area` = interaktivní výběr se dělá ve vlastním overlay okně (viz ShotService).
    public static byte[]? CaptureScreen(int x, int y, int w, int h)
    {
#if WINDOWS
        try
        {
            using var bmp = new System.Drawing.Bitmap(w, h);
            using (var g = System.Drawing.Graphics.FromImage(bmp)) g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
            using var ms = new MemoryStream(); bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png); return ms.ToArray();
        }
        catch (Exception e) { Log.W("capture: " + e.Message); }
#endif
        return null;
    }
    public static (int w, int h) ScreenSize()
    {
#if WINDOWS
        return (GetSystemMetrics(0), GetSystemMetrics(1));
#else
        return (1920, 1080);
#endif
    }

    /// OCR přes Windows.Media.Ocr (jazyk podle systému, zkusí i cs/en).
    public static async Task<string> OcrAsync(byte[] png)
    {
#if WINDOWS
        try
        {
            using var ms = new MemoryStream(png);
            var ras = ms.AsRandomAccessStream();
            var dec = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ras);
            var bmp = await dec.GetSoftwareBitmapAsync();
            var eng = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("cs")) ?? Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
            if (eng == null) return "";
            var res = await eng.RecognizeAsync(bmp);
            return string.Join("\n", res.Lines.Select(l => l.Text));
        }
        catch (Exception e) { Log.W("ocr: " + e.Message); }
#endif
        await Task.CompletedTask; return "";
    }

    public static void OpenUrl(string url) { try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { } }
    public static void Reveal(string path) { try { if (IsWindows) Process.Start("explorer.exe", $"/select,\"{path}\""); else Process.Start("open", $"-R \"{path}\""); } catch { } }
}
