using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

public partial class DevolucionPage : Page
{
    private readonly PrestamoNegocio _prestamos = new PrestamoNegocio();
    private bool _cargando;

    public DevolucionPage()
    {
        InitializeComponent();
        dpDevolucion.SelectedDate = DateTime.Today;
        Loaded += async (s, e) => await CargarAsync();
    }

    private DateTime FechaDevolucion => dpDevolucion.SelectedDate ?? DateTime.Today;

    private async Task CargarAsync()
    {
        var seleccion = dgPendientes.SelectedItem as LineaDevolucion;

        await UiServicios.RunAsync(async () =>
        {
            var lista = await _prestamos.ListarPendientesAsync(FechaDevolucion);
            _cargando = true;
            dgPendientes.ItemsSource = lista;
            emptyState.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (seleccion != null)
                dgPendientes.SelectedItem = lista.FirstOrDefault(l =>
                    l.PrestamoId == seleccion.PrestamoId && l.LibroId == seleccion.LibroId);
            _cargando = false;
        }, SetBusy);

        MostrarSeleccion();
    }

    private void AplicarFecha()
    {
        if (dgPendientes.ItemsSource is not IEnumerable<LineaDevolucion> lineas)
            return;

        foreach (var linea in lineas)
            _prestamos.CompletarMulta(linea, FechaDevolucion);

        dgPendientes.Items.Refresh();
        MostrarSeleccion();
    }

    private void MostrarSeleccion()
    {
        if (dgPendientes.SelectedItem is not LineaDevolucion linea)
        {
            txtDetalle.Text = "Seleccione un libro pendiente.";
            txtMulta.Text = "Multa: —";
            return;
        }

        txtDetalle.Text = linea.DiasRetraso > 0
            ? $"{linea.Libro} · {linea.DiasRetraso} día(s) de retraso"
            : $"{linea.Libro} · devuelto dentro del plazo";
        txtMulta.Text = "Multa: S/ " + linea.Multa.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private async Task RegistrarAsync()
    {
        if (dgPendientes.SelectedItem is not LineaDevolucion linea)
        {
            UiServicios.ShowWarning("Seleccione el libro que se devuelve.");
            return;
        }

        decimal multa = 0;
        bool ok = await UiServicios.RunAsync(async () =>
        {
            multa = await _prestamos.RegistrarDevolucionAsync(linea.PrestamoId, linea.LibroId, FechaDevolucion);
        }, SetBusy);

        if (!ok) return;

        UiServicios.ShowSuccess(multa > 0
            ? $"Devolución registrada. Multa: S/ {multa.ToString("0.00", CultureInfo.InvariantCulture)}."
            : "Devolución registrada. Sin multa.");
        await CargarAsync();
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnRegistrar.IsEnabled = !busy;
    }

    private void dgPendientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando) return;
        MostrarSeleccion();
    }

    private void dpDevolucion_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        AplicarFecha();
    }

    private async void btnRegistrar_Click(object sender, RoutedEventArgs e) => await RegistrarAsync();
}
