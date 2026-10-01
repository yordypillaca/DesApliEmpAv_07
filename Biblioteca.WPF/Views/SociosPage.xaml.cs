using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF.Views;

public partial class SociosPage : Page
{
    private readonly SocioNegocio _socios = new SocioNegocio();
    private readonly DispatcherTimer _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
    private Socio _editando;
    private bool _cargando;

    public SociosPage()
    {
        InitializeComponent();
        _searchTimer.Tick += async (s, e) =>
        {
            _searchTimer.Stop();
            await CargarAsync();
        };
        PreviewKeyDown += Page_PreviewKeyDown;
        Loaded += async (s, e) => await CargarAsync();
        Limpiar();
    }

    private async Task CargarAsync()
    {
        string texto = txtSearch.Text;
        int? id = _editando?.SocioId;

        await UiServicios.RunAsync(async () =>
        {
            var lista = await _socios.BuscarAsync(texto);
            _cargando = true;
            dgSocios.ItemsSource = lista;
            _cargando = false;

            txtEmpty.Text = string.IsNullOrWhiteSpace(texto)
                ? "No hay socios."
                : $"Ningún socio coincide con \"{texto}\".";
            emptyState.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (id != null)
            {
                _cargando = true;
                dgSocios.SelectedItem = lista.FirstOrDefault(s => s.SocioId == id);
                _cargando = false;
            }
        }, SetBusy);
    }

    private async Task GuardarAsync()
    {
        var socio = new Socio
        {
            SocioId = _editando?.SocioId ?? 0,
            DNI = txtDni.Text,
            Nombre = txtNombre.Text,
            Email = txtEmail.Text
        };
        bool esNuevo = socio.SocioId == 0;

        bool ok = await UiServicios.RunAsync(async () =>
        {
            if (esNuevo)
                await _socios.RegistrarAsync(socio);
            else
                await _socios.ActualizarAsync(socio);
        }, SetBusy);

        if (!ok) return;

        UiServicios.ShowSuccess(esNuevo
            ? $"{socio.Nombre} fue registrado."
            : $"{socio.Nombre} fue actualizado.");
        if (esNuevo) Limpiar();
        await CargarAsync();
    }

    private async Task EliminarAsync()
    {
        if (_editando == null) return;

        var socio = _editando;
        bool confirmado = await UiServicios.ConfirmAsync(
            "Dar de baja al socio",
            $"¿Dar de baja a {socio.Nombre} ({socio.DNI})?",
            "Dar de baja");
        if (!confirmado) return;

        bool ok = await UiServicios.RunAsync(
            async () => await _socios.EliminarAsync(socio.SocioId), SetBusy);
        if (!ok) return;

        UiServicios.ShowSuccess($"{socio.Nombre} fue dado de baja.");
        Limpiar();
        await CargarAsync();
    }

    private void Editar(Socio socio)
    {
        _editando = socio;
        txtDni.Text = socio.DNI;
        txtNombre.Text = socio.Nombre;
        txtEmail.Text = socio.Email;
        txtFormTitle.Text = "Editar socio";
        txtFormHint.Text = "Pulse Esc para cancelar la edición.";
        iconForm.Symbol = SymbolRegular.PersonEdit24;
        ActualizarBotones();
    }

    private void Limpiar()
    {
        _editando = null;
        dgSocios.SelectedItem = null;
        txtDni.Clear();
        txtNombre.Clear();
        txtEmail.Clear();
        txtFormTitle.Text = "Nuevo socio";
        txtFormHint.Text = "Complete los datos y pulse Guardar.";
        iconForm.Symbol = SymbolRegular.PersonAdd24;
        ActualizarBotones();
        txtDni.Focus();
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnSave.IsEnabled = !busy;
        btnNew.IsEnabled = !busy;
        btnRefresh.IsEnabled = !busy;
        ActualizarBotones(busy);
    }

    private void ActualizarBotones(bool busy = false) =>
        btnDelete.IsEnabled = !busy && _editando != null;

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private async void btnRefresh_Click(object sender, RoutedEventArgs e) => await CargarAsync();

    private void dgSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando) return;
        if (dgSocios.SelectedItem is Socio socio)
            Editar(socio);
    }

    private void btnNew_Click(object sender, RoutedEventArgs e) => Limpiar();

    private async void btnSave_Click(object sender, RoutedEventArgs e) => await GuardarAsync();

    private async void btnDelete_Click(object sender, RoutedEventArgs e) => await EliminarAsync();

    private async void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S && btnSave.IsEnabled)
        {
            e.Handled = true;
            await GuardarAsync();
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            e.Handled = true;
            Limpiar();
        }
        else if (e.Key == Key.Escape && _editando != null)
        {
            e.Handled = true;
            Limpiar();
        }
    }
}
