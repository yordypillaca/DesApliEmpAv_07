using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF.Views;

public partial class LibrosPage : Page
{
    private readonly LibroNegocio _libros = new LibroNegocio();
    private readonly DispatcherTimer _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
    private Libro _editando;
    private bool _cargando;

    public LibrosPage()
    {
        InitializeComponent();
        _searchTimer.Tick += async (s, e) =>
        {
            _searchTimer.Stop();
            await CargarAsync();
        };
        PreviewKeyDown += Page_PreviewKeyDown;
        Loaded += async (s, e) =>
        {
            await CargarAutoresAsync();
            await CargarAsync();
        };
        Limpiar();
    }

    private async Task CargarAutoresAsync()
    {
        await UiServicios.RunAsync(async () =>
        {
            var autores = await _libros.ListarAutoresAsync();
            cboAutor.ItemsSource = autores;
        }, SetBusy);
    }

    private async Task CargarAsync()
    {
        string texto = txtSearch.Text;
        int? id = _editando?.LibroId;

        await UiServicios.RunAsync(async () =>
        {
            var lista = await _libros.BuscarAsync(texto);
            _cargando = true;
            dgLibros.ItemsSource = lista;
            _cargando = false;

            txtEmpty.Text = string.IsNullOrWhiteSpace(texto)
                ? "No hay libros."
                : $"Ningún libro coincide con \"{texto}\".";
            emptyState.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (id != null)
            {
                _cargando = true;
                dgLibros.SelectedItem = lista.FirstOrDefault(l => l.LibroId == id);
                _cargando = false;
            }
        }, SetBusy);
    }

    private async Task GuardarAsync()
    {
        var libro = new Libro
        {
            LibroId = _editando?.LibroId ?? 0,
            Titulo = txtTitulo.Text,
            ISBN = txtIsbn.Text,
            AutorId = cboAutor.SelectedValue is int id ? id : 0,
            Ejemplares = nbEjemplares.Value is double cantidad ? (int)cantidad : -1
        };
        bool esNuevo = libro.LibroId == 0;

        bool ok = await UiServicios.RunAsync(async () =>
        {
            if (esNuevo)
                await _libros.RegistrarAsync(libro);
            else
                await _libros.ActualizarAsync(libro);
        }, SetBusy);

        if (!ok) return;

        UiServicios.ShowSuccess(esNuevo ? "El libro fue registrado." : "El libro fue actualizado.");
        if (esNuevo) Limpiar();
        await CargarAsync();
    }

    private async Task EliminarAsync()
    {
        if (_editando == null) return;

        var libro = _editando;
        bool confirmado = await UiServicios.ConfirmAsync(
            "Dar de baja el libro",
            $"¿Dar de baja \"{libro.Titulo}\"?",
            "Dar de baja");
        if (!confirmado) return;

        bool ok = await UiServicios.RunAsync(
            async () => await _libros.EliminarAsync(libro.LibroId), SetBusy);
        if (!ok) return;

        UiServicios.ShowSuccess($"\"{libro.Titulo}\" fue dado de baja.");
        Limpiar();
        await CargarAsync();
    }

    private void Editar(Libro libro)
    {
        _editando = libro;
        txtTitulo.Text = libro.Titulo;
        txtIsbn.Text = libro.ISBN;
        cboAutor.SelectedValue = libro.AutorId;
        nbEjemplares.Value = libro.Ejemplares;
        txtFormTitle.Text = "Editar libro";
        txtFormHint.Text = "Pulse Esc para cancelar la edición.";
        iconForm.Symbol = SymbolRegular.Edit24;
        ActualizarBotones();
    }

    private void Limpiar()
    {
        _editando = null;
        dgLibros.SelectedItem = null;
        txtTitulo.Clear();
        txtIsbn.Clear();
        cboAutor.SelectedIndex = -1;
        nbEjemplares.Value = 1;
        txtFormTitle.Text = "Nuevo libro";
        txtFormHint.Text = "Complete los datos y pulse Guardar.";
        iconForm.Symbol = SymbolRegular.BookAdd24;
        ActualizarBotones();
        txtTitulo.Focus();
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

    private void dgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando) return;
        if (dgLibros.SelectedItem is Libro libro)
            Editar(libro);
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
