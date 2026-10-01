using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class SocioDatos
{
    private const string SelectColumns =
        "SELECT SocioId, DNI, Nombre, Email, Activo FROM Socios";

    public async Task<List<Socio>> ListarAsync()
    {
        const string sql = SelectColumns + " WHERE Activo = 1 ORDER BY Nombre";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<List<Socio>> BuscarAsync(string texto)
    {
        const string sql = SelectColumns + @"
            WHERE Activo = 1
              AND (Nombre LIKE @Texto OR DNI LIKE @Texto)
            ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Texto", DbConnectionFactory.Contiene(texto));
        await cn.OpenAsync();
        return await LeerTodosAsync(cmd);
    }

    public async Task<Socio> ObtenerPorIdAsync(int socioId)
    {
        const string sql = SelectColumns + " WHERE SocioId = @Id";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        await cn.OpenAsync();
        return (await LeerTodosAsync(cmd)).FirstOrDefault();
    }

    public async Task<bool> ExisteDniAsync(string dni, int excluirSocioId)
    {
        const string sql = @"SELECT COUNT(1) FROM Socios
                             WHERE DNI = @DNI AND SocioId <> @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@DNI", dni);
        cmd.Parameters.AddWithValue("@Id", excluirSocioId);
        await cn.OpenAsync();
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task InsertarAsync(Socio socio)
    {
        const string sql = @"INSERT INTO Socios (DNI, Nombre, Email)
                             VALUES (@DNI, @Nombre, @Email)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ActualizarAsync(Socio socio)
    {
        const string sql = @"UPDATE Socios
                             SET DNI = @DNI, Nombre = @Nombre, Email = @Email
                             WHERE SocioId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        cmd.Parameters.AddWithValue("@Id", socio.SocioId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EliminarLogicoAsync(int socioId)
    {
        const string sql = "UPDATE Socios SET Activo = 0 WHERE SocioId = @Id";
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", socioId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AgregarParametros(SqlCommand cmd, Socio socio)
    {
        cmd.Parameters.AddWithValue("@DNI", socio.DNI);
        cmd.Parameters.AddWithValue("@Nombre", socio.Nombre);
        cmd.Parameters.AddWithValue("@Email", (object)socio.Email ?? DBNull.Value);
    }

    private static async Task<List<Socio>> LeerTodosAsync(SqlCommand cmd)
    {
        var lista = new List<Socio>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Socio
            {
                SocioId = dr.GetInt32(0),
                DNI = dr.GetString(1),
                Nombre = dr.GetString(2),
                Email = dr.IsDBNull(3) ? null : dr.GetString(3),
                Activo = dr.GetBoolean(4)
            });
        }
        return lista;
    }
}
