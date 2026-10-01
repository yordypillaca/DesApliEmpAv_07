using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class AutorDatos
{
    private const string SelectColumns =
        "SELECT AutorId, Nombre, Nacionalidad, Activo FROM Autores";

    public async Task<List<Autor>> ListarAsync()
    {
        const string sql = SelectColumns + " WHERE Activo = 1 ORDER BY Nombre";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<Autor> ObtenerPorIdAsync(int autorId)
    {
        const string sql = SelectColumns + " WHERE AutorId = @Id";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", autorId);
        await cn.OpenAsync();
        return (await LeerTodosAsync(cmd)).FirstOrDefault();
    }

    public async Task InsertarAsync(Autor autor)
    {
        const string sql = @"INSERT INTO Autores (Nombre, Nacionalidad)
                             VALUES (@Nombre, @Nacionalidad)";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Nombre", autor.Nombre);
        cmd.Parameters.AddWithValue("@Nacionalidad", autor.Nacionalidad);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ActualizarAsync(Autor autor)
    {
        const string sql = @"UPDATE Autores
                             SET Nombre = @Nombre, Nacionalidad = @Nacionalidad
                             WHERE AutorId = @Id";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Nombre", autor.Nombre);
        cmd.Parameters.AddWithValue("@Nacionalidad", autor.Nacionalidad);
        cmd.Parameters.AddWithValue("@Id", autor.AutorId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EliminarLogicoAsync(int autorId)
    {
        const string sql = "UPDATE Autores SET Activo = 0 WHERE AutorId = @Id";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", autorId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<Autor>> LeerTodosAsync(SqlCommand cmd)
    {
        var lista = new List<Autor>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Autor
            {
                AutorId = dr.GetInt32(0),
                Nombre = dr.GetString(1),
                Nacionalidad = dr.GetString(2),
                Activo = dr.GetBoolean(3)
            });
        }
        return lista;
    }
}
