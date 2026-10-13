using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NotchIsland.Core;

namespace NotchIsland.Views;

/// Vlastní hláška „NotchIsland už běží“ (místo výchozího Windows MessageBoxu).
public class AlreadyRunningWindow : Window
{
    public AlreadyRunningWindow()
    {
        Title = "NotchIsland"; SystemDecorations = WindowDecorations.None; CanResize = false; SizeToContent = SizeToContent.WidthAndHeight; Topmost = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen; Background = Brushes.Transparent; TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Image? logo = null; try { logo = new Image { Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(new Uri("avares://NotchIsland/Assets/icon.png"))), Width = 56, Height = 56 }; } catch { }
        var ok = new Button { Classes = { "chip" }, Content = L.T("OK"), Padding = new Thickness(28, 7), FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Background = new SolidColorBrush(Color.Parse("#3FE36F")), Foreground = Brushes.Black };
        ok.Click += (_, _) => Close();
        var col = new StackPanel { Spacing = 10, Width = 300, HorizontalAlignment = HorizontalAlignment.Center };
        if (logo != null) col.Children.Add(logo);
        col.Children.Add(new TextBlock { Text = L.T("NotchIsland is already running"), FontSize = 15, FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center });
        col.Children.Add(new TextBlock { Text = L.T("You can only have it open once. Look for the island at the top edge of the screen or the icon in the system tray."), FontSize = 12, Classes = { "muted" }, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
        col.Children.Add(new Border { Height = 6 });
        col.Children.Add(ok);
        Content = new Border { Background = new SolidColorBrush(Color.Parse("#161619")), BorderBrush = new SolidColorBrush(Color.Parse("#2A2A30")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16), Padding = new Thickness(28, 26), Margin = new Thickness(12), BoxShadow = new BoxShadows(new BoxShadow { Blur = 24, OffsetY = 8, Color = Color.Parse("#80000000") }), Child = col };
        KeyDown += (_, e) => { if (e.Key is Avalonia.Input.Key.Enter or Avalonia.Input.Key.Escape) Close(); };
    }
}
