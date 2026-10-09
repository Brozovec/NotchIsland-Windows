using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace NotchIsland.Views;

/// Font Awesome 6 Free – stejné ikony jako v macOS verzi (fonty v Assets/Fonts).
public static class FA
{
    public static readonly FontFamily Solid = new("avares://NotchIsland/Assets/Fonts/fa-solid-900.ttf#Font Awesome 6 Free");
    public static readonly FontFamily Brands = new("avares://NotchIsland/Assets/Fonts/fa-brands-400.ttf#Font Awesome 6 Brands");
    // solid
    public const string House = "", Inbox = "", Train = "", Tram = "", Bus = "", TrainSide = "", Phone = "", Camera = "", Note = "", Clipboard = "",
        Stopwatch = "", Graduation = "", Gear = "", Star = "", Cart = "", Search = "", Rotate = "", Swap = "", Location = "", Mug = "", Book = "",
        Play = "", Pause = "", Backward = "", Forward = "", Plus = "+", Stop = "", Leaf = "", Pin = "", Xmark = "", Trash = "", Copy = "", AlignLeft = "",
        ChevronLeft = "", ChevronRight = "", Crop = "", Display = "", TextIcon = "", Mic = "", MicSlash = "", Folder = "", FileImage = "", FilePdf = "", FileZip = "",
        FileVideo = "", FileAudio = "", FileWord = "", FileExcel = "", File = "", Sun = "", Moon = "", CloudSun = "", CloudMoon = "", Cloud = "", Smog = "", Rain = "", Snow = "", Bolt = "", Check = "", Sunrise = "", Bell = "";
    // brands
    public const string Spotify = "", Apple = "", Chrome = "", Windows = "", Discord = "", Microsoft = "", Slack = "", Telegram = "", Whatsapp = "", Skype = "", Youtube = "";

    public static TextBlock Icon(string glyph, double size = 12, bool brand = false, IBrush? color = null) => new()
    {
        Text = glyph, FontFamily = brand ? Brands : Solid, FontSize = size, Foreground = color ?? Brushes.White,
        VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center
    };
    /// Ikona + text vedle sebe (pro tlačítka)
    public static StackPanel Label(string glyph, string text, double size = 11, bool brand = false, IBrush? color = null) => new()
    {
        Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center,
        Children = { Icon(glyph, size, brand, color), new TextBlock { Text = text, FontSize = size, FontWeight = FontWeight.SemiBold, Foreground = color ?? Brushes.White, VerticalAlignment = VerticalAlignment.Center } }
    };
    public static string Weather(int code, bool day) => code switch { 0 => day ? Sun : Moon, 1 or 2 => day ? CloudSun : CloudMoon, 3 => Cloud, 45 or 48 => Smog, >= 51 and <= 67 or >= 80 and <= 82 => Rain, >= 71 and <= 77 or 85 or 86 => Snow, >= 95 => Bolt, _ => Cloud };
    public static string FileIcon(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" => FileImage, ".pdf" => FilePdf, ".zip" or ".rar" or ".7z" => FileZip, ".mp4" or ".mov" or ".mkv" => FileVideo, ".mp3" or ".wav" => FileAudio, ".docx" or ".doc" => FileWord, ".xlsx" => FileExcel, _ => File };
}
