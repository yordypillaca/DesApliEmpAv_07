using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class LibroRepositorio
{
    private const string SelectColumns = @"
        SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, a.Nombre, l.Ejemplares, l.Activo
        FROM Libros l
        INNER JOIN Autores a ON a.AutorId = l.AutorId";

    public async Task<List<Libro>> ListarAsync()
    {
        const string sql = SelectColumns + " WHERE l.Activo = 1 ORDER BY l.Titulo";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<List<Libro>> BuscarAsync(string texto)
    {
        const string sql = SelectColumns + @"
            WHERE l.Activo = 1
              AND (l.Titulo LIKE @Texto OR a.Nombre LIKE @Texto)
            ORDER BY l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Texto", DbConnectionFactory.Contiene(texto));
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<List<Libro>> ListarDisponiblesAsync()
    {
        const string sql = SelectColumns + @"
            WHERE l.Activo = 1 AND l.Ejemplares > 0
            ORDER BY l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<Libro> ObtenerPorIdAsync(int libroId)
    {
        const string sql = SelectColumns + " WHERE l.LibroId = @Id";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        await cn.OpenAsync();
        return (await LeerTodosAsync(cmd)).FirstOrDefault();
    }

    public async Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId)
    {
        const string sql = @"SELECT COUNT(1) FROM Libros
                             WHERE ISBN = @ISBN AND LibroId <> @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@ISBN", isbn);
        cmd.Parameters.AddWithValue("@Id", excluirLibroId);
        await cn.OpenAsync();
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<bool> TienePrestamosPendientesAsync(int libroId)
    {
        const string sql = @"SELECT COUNT(1) FROM DetallePrestamo
                             WHERE LibroId = @Id AND FechaDevolucion IS NULL";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        await cn.OpenAsync();
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task InsertarAsync(Libro libro)
    {
        const string sql = @"INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares)
                             VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ActualizarAsync(Libro libro)
    {
        const string sql = @"UPDATE Libros
                             SET Titulo = @Titulo, ISBN = @ISBN, AutorId = @AutorId, Ejemplares = @Ejemplares
                             WHERE LibroId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        cmd.Parameters.AddWithValue("@Id", libro.LibroId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EliminarLogicoAsync(int libroId)
    {
        const string sql = "UPDATE Libros SET Activo = 0 WHERE LibroId = @Id";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", libroId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    internal async Task<bool> DescontarEjemplarAsync(int libroId, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"UPDATE Libros
                             SET Ejemplares = Ejemplares - 1
                             WHERE LibroId = @Id AND Activo = 1 AND Ejemplares > 0";

        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@Id", libroId);
        return await cmd.ExecuteNonQueryAsync() == 1;
    }

    internal async Task<bool> ReponerEjemplarAsync(int libroId, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"UPDATE Libros
                             SET Ejemplares = Ejemplares + 1
                             WHERE LibroId = @Id";

        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@Id", libroId);
        return await cmd.ExecuteNonQueryAsync() == 1;
    }

    private static void AgregarParametros(SqlCommand cmd, Libro libro)
    {
        cmd.Parameters.AddWithValue("@Titulo", libro.Titulo);
        cmd.Parameters.AddWithValue("@ISBN", libro.ISBN);
        cmd.Parameters.AddWithValue("@AutorId", libro.AutorId);
        cmd.Parameters.AddWithValue("@Ejemplares", libro.Ejemplares);
    }

    private static async Task<List<Libro>> LeerTodosAsync(SqlCommand cmd)
    {
        var lista = new List<Libro>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Libro
            {
                LibroId = dr.GetInt32(0),
                Titulo = dr.GetString(1),
                ISBN = dr.GetString(2),
                AutorId = dr.GetInt32(3),
                NombreAutor = dr.GetString(4),
                Ejemplares = dr.GetInt32(5),
                Activo = dr.GetBoolean(6)
            });
        }
        return lista;
    }
}
