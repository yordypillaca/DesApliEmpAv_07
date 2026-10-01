using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class LibroNegocio
{
    private readonly ILibroRepositorio _libros;
    private readonly AutorDatos _autores;

    public LibroNegocio() : this(new LibroRepositorioAdaptador(), new AutorDatos()) { }

    public LibroNegocio(ILibroRepositorio libros) : this(libros, new AutorDatos()) { }

    public LibroNegocio(ILibroRepositorio libros, AutorDatos autores)
    {
        _libros = libros ?? throw new ArgumentNullException(nameof(libros));
        _autores = autores ?? throw new ArgumentNullException(nameof(autores));
    }

    public Task<List<Autor>> ListarAutoresAsync() => _autores.ListarAsync();

    public Task<List<Libro>> ListarAsync() => _libros.ListarAsync();

    public Task<List<Libro>> BuscarAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? ListarAsync()
            : _libros.BuscarAsync(texto.Trim());

    public async Task RegistrarAsync(Libro libro)
    {
        await ValidarAsync(libro);
        await _libros.InsertarAsync(libro);
    }

    public async Task ActualizarAsync(Libro libro)
    {
        if (libro.LibroId <= 0)
            throw new ReglaNegocioException("Seleccione un libro para actualizar.");

        await ValidarAsync(libro);
        await _libros.ActualizarAsync(libro);
    }

    public async Task EliminarAsync(int libroId)
    {
        if (libroId <= 0)
            throw new ReglaNegocioException("Seleccione un libro.");

        if (await _libros.TienePrestamosPendientesAsync(libroId))
            throw new ReglaNegocioException("No se puede dar de baja un libro con préstamos pendientes.");

        await _libros.EliminarLogicoAsync(libroId);
    }

    private async Task ValidarAsync(Libro libro)
    {
        libro.Titulo = libro.Titulo?.Trim();
        libro.ISBN = libro.ISBN?.Trim();

        if (string.IsNullOrWhiteSpace(libro.Titulo))
            throw new ReglaNegocioException("El título es obligatorio.");

        if (libro.Titulo.Length > 200)
            throw new ReglaNegocioException("El título no puede superar 200 caracteres.");

        if (string.IsNullOrWhiteSpace(libro.ISBN))
            throw new ReglaNegocioException("El ISBN es obligatorio.");

        if (libro.ISBN.Length > 20)
            throw new ReglaNegocioException("El ISBN no puede superar 20 caracteres.");

        if (libro.Ejemplares < 0)
            throw new ReglaNegocioException("La cantidad de ejemplares no puede ser negativa.");

        Autor autor = await _autores.ObtenerPorIdAsync(libro.AutorId);
        if (autor == null || !autor.Activo)
            throw new ReglaNegocioException("Seleccione un autor activo.");

        if (await _libros.ExisteIsbnAsync(libro.ISBN, libro.LibroId))
            throw new ReglaNegocioException($"El ISBN '{libro.ISBN}' ya está registrado.");
    }

    private sealed class LibroRepositorioAdaptador : ILibroRepositorio
    {
        private readonly LibroRepositorio _datos = new LibroRepositorio();

        public Task<List<Libro>> ListarAsync() => _datos.ListarAsync();
        public Task<List<Libro>> BuscarAsync(string texto) => _datos.BuscarAsync(texto);
        public Task<Libro> ObtenerPorIdAsync(int libroId) => _datos.ObtenerPorIdAsync(libroId);
        public Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId) => _datos.ExisteIsbnAsync(isbn, excluirLibroId);
        public Task<bool> TienePrestamosPendientesAsync(int libroId) => _datos.TienePrestamosPendientesAsync(libroId);
        public Task InsertarAsync(Libro libro) => _datos.InsertarAsync(libro);
        public Task ActualizarAsync(Libro libro) => _datos.ActualizarAsync(libro);
        public Task EliminarLogicoAsync(int libroId) => _datos.EliminarLogicoAsync(libroId);
    }
}
