using CommunityToolkit.Mvvm.ComponentModel;
using NotchIsland.Core;

namespace NotchIsland.Services;

public record Shot(string Path, byte[] Png, DateTime Taken);

/// Screenshoty: celá obrazovka / výběr oblasti (overlay) / OCR. Snímek jde do schránky i do složky.
public sealed partial class ShotService : ObservableObject
{
    public static ShotService Instance { get; } = new();
    [ObservableProperty] List<Shot> shots = new();
    [ObservableProperty] string lastOcr = "";
    public Func<Task<(int x, int y, int w, int h)?>>? SelectArea;   // UI: overlay pro výběr oblasti
    public Func<byte[], Task>? CopyImage;                          // UI: obrázek do schránky
    public Func<string, Task>? CopyText;
    public event Action<string>? Toast;

    public async Task CaptureAsync(string mode)
    {
        (int x, int y, int w, int h) rect;
        if (mode == "screen") { var s = Platform.ScreenSize(); rect = (0, 0, s.w, s.h); }
        else { var r = SelectArea != null ? await SelectArea() : null; if (r == null) return; rect = r.Value; }
        var png = Platform.CaptureScreen(rect.x, rect.y, rect.w, rect.h); if (png == null) { Toast?.Invoke("Capture failed"); return; }
        if (mode == "ocr")
        {
            var text = await Platform.OcrAsync(png); LastOcr = text;
            if (CopyText != null) await CopyText(text);
            Toast?.Invoke(text == "" ? "No text found" : L.T("Text copied")); return;
        }
        Directory.CreateDirectory(Settings.Current.ScreenshotFolder);
        var path = Path.Combine(Settings.Current.ScreenshotFolder, $"Screenshot {DateTime.Now:yyyy-MM-dd HH.mm.ss}.png");
        await File.WriteAllBytesAsync(path, png);
        if (CopyImage != null) await CopyImage(png);
        Shots = new[] { new Shot(path, png, DateTime.Now) }.Concat(Shots).Take(12).ToList();
        Toast?.Invoke(L.T("Copied to clipboard"));
    }
}
