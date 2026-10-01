using System.Windows;
using System.Windows.Controls;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

public partial class ReportePage : Page
{
    private readonly PrestamoNegocio _prestamos = new PrestamoNegocio();

    public ReportePage()
    {
        InitializeComponent();
        dpDesde.SelectedDate = DateTime.Today.AddDays(-30);
        dpHasta.SelectedDate = DateTime.Today;
        Loaded += async (s, e) => await ConsultarAsync();
    }

    private async Task ConsultarAsync()
    {
        DateTime desde = dpDesde.SelectedDate ?? default;
        DateTime hasta = dpHasta.SelectedDate ?? default;

        await UiServicios.RunAsync(async () =>
        {
            var lista = await _prestamos.ReporteAsync(desde, hasta);
            dgReporte.ItemsSource = lista;
            emptyState.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtResumen.Text = lista.Count == 1
                ? "1 libro en el intervalo."
                : $"{lista.Count} libros en el intervalo.";
        }, SetBusy);
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnConsultar.IsEnabled = !busy;
    }

    private async void btnConsultar_Click(object sender, RoutedEventArgs e) => await ConsultarAsync();
}
