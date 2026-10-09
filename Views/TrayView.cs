using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using NotchIsland.Core;

namespace NotchIsland.Views;

/// Odkladiště souborů: přetáhni dovnitř (kopie do %APPDATA%\NotchIsland\Shelf), dvojklik otevře, × smaže.
public class TrayView : UserControl
{
    static string Dir => Path.Combine(Settings.Dir, "Shelf");
    readonly WrapPanel items = new() { Orientation = Orientation.Horizontal };
    readonly TextBlock empty = new() { Text = L.T("Drop files here"), Classes = { "muted" }, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    public TrayView()
    {
        Directory.CreateDirectory(Dir);
        var border = new Border { CornerRadius = new CornerRadius(12), BorderBrush = new SolidColorBrush(Color.Parse("#40FFFFFF")), BorderThickness = new Thickness(1), Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")), Child = new Grid { Children = { empty, new ScrollViewer { Content = items, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto } } } };
        DragDrop.SetAllowDrop(border, true);
        border.AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            var files = e.DataTransfer?.TryGetFiles();
            if (files == null) return;
            foreach (var f in files) { try { var p = f.TryGetLocalPath(); if (p != null && File.Exists(p)) { var dest = Path.Combine(Dir, Path.GetFileName(p)); File.Copy(p, dest, true); } } catch { } }
            Reload();
        });
        border.AddHandler(DragDrop.DragOverEvent, (_, e) => { e.DragEffects = DragDropEffects.Copy; });
        Content = border; Reload();
    }
    void Reload()
    {
        items.Children.Clear();
        var files = Directory.GetFiles(Dir).OrderByDescending(File.GetLastWriteTime).ToList();
        empty.IsVisible = files.Count == 0;
        foreach (var f in files)
        {
            var name = new TextBlock { Text = Path.GetFileName(f), FontSize = 9, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 70 };
            var icon = new TextBlock { Text = IconFor(f), FontSize = 26, HorizontalAlignment = HorizontalAlignment.Center };
            var del = new Button { Classes = { "icon" }, Content = "×", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(3, 0) };
            del.Click += (_, _) => { try { File.Delete(f); } catch { } Reload(); };
            var cell = new Grid { Width = 74, Margin = new Thickness(4), Children = { new StackPanel { Spacing = 2, Children = { icon, name } }, del } };
            cell.DoubleTapped += (_, _) => Services.Platform.OpenUrl(f);
            cell.PointerPressed += async (_, e) =>
            {
                if (!e.GetCurrentPoint(cell).Properties.IsLeftButtonPressed) return;
                var sp = TopLevel.GetTopLevel(this)?.StorageProvider; if (sp == null) return;
                var file = await sp.TryGetFileFromPathAsync(new Uri(f)); if (file == null) return;
                var d = new DataTransfer(); d.Add(DataTransferItem.Create(DataFormat.File, file));
                await DragDrop.DoDragDropAsync(e, d, DragDropEffects.Copy);
            };
            items.Children.Add(cell);
        }
    }
    static string IconFor(string f) => Path.GetExtension(f).ToLowerInvariant() switch { ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" => "🖼️", ".pdf" => "📕", ".zip" or ".rar" or ".7z" => "🗜️", ".mp4" or ".mov" or ".mkv" => "🎬", ".mp3" or ".wav" => "🎵", ".docx" or ".doc" => "📝", ".xlsx" => "📊", _ => "📄" };
}
