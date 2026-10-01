using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

public partial class PrestamoPage : Page
{
    private readonly PrestamoNegocio _prestamos = new PrestamoNegocio();
    private readonly ObservableCollection<Libro> _carrito = new ObservableCollection<Libro>();
    private bool _cargandoSocios;

    public PrestamoPage()
    {
        InitializeComponent();
        dgCarrito.ItemsSource = _carrito;
        Loaded += async (s, e) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        int socioId = cboSocio.SelectedValue is int id ? id : 0;

        await UiServicios.RunAsync(async () =>
        {
            var socios = await _prestamos.ListarSociosAsync();
            var libros = await _prestamos.ListarLibrosDisponiblesAsync();

            _cargandoSocios = true;
            cboSocio.ItemsSource = socios;
            if (socioId > 0)
                cboSocio.SelectedValue = socioId;
            else
                cboSocio.SelectedIndex = -1;
            _cargandoSocios = false;

            cboLibro.ItemsSource = libros;
            cboLibro.SelectedIndex = -1;

            if (dpPrestamo.SelectedDate == null)
                dpPrestamo.SelectedDate = DateTime.Today;
            if (dpLimite.SelectedDate == null)
                dpLimite.SelectedDate = DateTime.Today.AddDays(7);
        }, SetBusy);

        await MostrarPendientesAsync();
    }

    private async Task MostrarPendientesAsync()
    {
        if (cboSocio.SelectedItem is not Socio socio)
        {
            txtPendientes.Text = "Seleccione un socio.";
            return;
        }

        await UiServicios.RunAsync(async () =>
        {
            int pendientes = await _prestamos.ContarPendientesAsync(socio.SocioId);
            txtPendientes.Text = $"{socio.Nombre} tiene {pendientes} de {_prestamos.MaxLibrosPendientes} libros pendientes.";
        }, SetBusy);
    }

    private async Task RegistrarAsync()
    {
        int socioId = cboSocio.SelectedValue is int id ? id : 0;
        var libros = _carrito.Select(l => l.LibroId).ToList();
        DateTime fechaPrestamo = dpPrestamo.SelectedDate ?? default;
        DateTime fechaLimite = dpLimite.SelectedDate ?? default;

        bool ok = await UiServicios.RunAsync(
            async () => await _prestamos.RegistrarAsync(socioId, fechaPrestamo, fechaLimite, libros),
            SetBusy);
        if (!ok) return;

        UiServicios.ShowSuccess($"Préstamo registrado con {libros.Count} libro(s).");
        _carrito.Clear();
        await CargarAsync();
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnRegistrar.IsEnabled = !busy;
        btnAgregar.IsEnabled = !busy;
        btnQuitar.IsEnabled = !busy;
    }

    private async void cboSocio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargandoSocios || !IsLoaded) return;
        await MostrarPendientesAsync();
    }

    private void btnAgregar_Click(object sender, RoutedEventArgs e)
    {
        if (cboLibro.SelectedItem is not Libro libro)
        {
            UiServicios.ShowWarning("Seleccione un libro.");
            return;
        }

        if (_carrito.Any(l => l.LibroId == libro.LibroId))
        {
            UiServicios.ShowWarning("Ese libro ya está en el préstamo.");
            return;
        }

        _carrito.Add(libro);
    }

    private void btnQuitar_Click(object sender, RoutedEventArgs e)
    {
        if (dgCarrito.SelectedItem is Libro libro)
            _carrito.Remove(libro);
    }

    private async void btnRegistrar_Click(object sender, RoutedEventArgs e) => await RegistrarAsync();
}
