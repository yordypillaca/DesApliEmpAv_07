using System.Windows;
using Biblioteca.WPF.Services;
using Biblioteca.WPF.Views;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        UiServicios.Initialize(SnackbarPresenter, RootDialogHost, RootNavigation);

        Loaded += (s, e) =>
        {
            RootNavigation.Navigate(typeof(LibrosPage));
            UpdateThemeIcon();
        };
    }

    private void btnTheme_Click(object sender, RoutedEventArgs e)
    {
        var next = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        App.SetTheme(next);
        UpdateThemeIcon();
    }

    private void UpdateThemeIcon()
    {
        bool isDark = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
        btnTheme.Icon = new SymbolIcon(isDark ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24);
    }
}
