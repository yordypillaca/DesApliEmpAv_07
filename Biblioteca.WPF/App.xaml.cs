using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplicationThemeManager.ApplySystemTheme(false);
        SetTheme(ApplicationThemeManager.GetAppTheme());
    }

    public static void SetTheme(ApplicationTheme theme)
    {
        ApplyAccent(theme);
        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, false);
        ApplyAccent(theme);
    }

    private static void ApplyAccent(ApplicationTheme theme)
    {
        if (theme == ApplicationTheme.Dark)
            ApplicationAccentColorManager.Apply(Rgb(0x14, 0xB8, 0xA6),
                Rgb(0x2D, 0xD4, 0xBF), Rgb(0x5E, 0xEA, 0xD4), Rgb(0x99, 0xF6, 0xE4));
        else
            ApplicationAccentColorManager.Apply(Rgb(0x0F, 0x76, 0x6E),
                Rgb(0x0F, 0x76, 0x6E), Rgb(0x11, 0x5E, 0x59), Rgb(0x13, 0x4E, 0x4A));
    }

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}
