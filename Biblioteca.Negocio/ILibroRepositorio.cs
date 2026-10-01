using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public interface ILibroRepositorio
{
    Task<List<Libro>> ListarAsync();
    Task<List<Libro>> BuscarAsync(string texto);
    Task<Libro> ObtenerPorIdAsync(int libroId);
    Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId);
    Task<bool> TienePrestamosPendientesAsync(int libroId);
    Task InsertarAsync(Libro libro);
    Task ActualizarAsync(Libro libro);
    Task EliminarLogicoAsync(int libroId);
}
