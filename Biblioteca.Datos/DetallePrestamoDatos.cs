using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class DetallePrestamoDatos
{
    public async Task<List<DetallePrestamo>> ListarPorPrestamoAsync(int prestamoId)
    {
        const string sql = @"
            SELECT d.PrestamoId, d.LibroId, l.Titulo, d.FechaDevolucion
            FROM DetallePrestamo d
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            WHERE d.PrestamoId = @Id
            ORDER BY l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", prestamoId);
        await cn.OpenAsync();

        var lista = new List<DetallePrestamo>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new DetallePrestamo
            {
                PrestamoId = dr.GetInt32(0),
                LibroId = dr.GetInt32(1),
                TituloLibro = dr.GetString(2),
                FechaDevolucion = dr.IsDBNull(3) ? null : dr.GetDateTime(3)
            });
        }
        return lista;
    }

    public Task InsertarAsync(DetallePrestamo detalle) =>
        EjecutarAsync(detalle, null, null);

    internal Task InsertarAsync(DetallePrestamo detalle, SqlConnection cn, SqlTransaction tx) =>
        EjecutarAsync(detalle, cn, tx);

    public async Task ActualizarFechaDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion)
    {
        const string sql = @"UPDATE DetallePrestamo
                             SET FechaDevolucion = @Fecha
                             WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Fecha", fechaDevolucion.Date);
        cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
        cmd.Parameters.AddWithValue("@LibroId", libroId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    internal async Task<bool> MarcarDevueltoAsync(int prestamoId, int libroId, DateTime fechaDevolucion, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"UPDATE DetallePrestamo
                             SET FechaDevolucion = @Fecha
                             WHERE PrestamoId = @PrestamoId
                               AND LibroId = @LibroId
                               AND FechaDevolucion IS NULL";

        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@Fecha", fechaDevolucion.Date);
        cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
        cmd.Parameters.AddWithValue("@LibroId", libroId);
        return await cmd.ExecuteNonQueryAsync() == 1;
    }

    private static async Task EjecutarAsync(DetallePrestamo detalle, SqlConnection cnExterna, SqlTransaction tx)
    {
        const string sql = @"INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion)
                             VALUES (@PrestamoId, @LibroId, @Fecha)";

        bool propia = cnExterna == null;
        var cn = cnExterna ?? DbConnectionFactory.Create();
        try
        {
            using var cmd = tx == null ? new SqlCommand(sql, cn) : new SqlCommand(sql, cn, tx);
            cmd.Parameters.AddWithValue("@PrestamoId", detalle.PrestamoId);
            cmd.Parameters.AddWithValue("@LibroId", detalle.LibroId);
            cmd.Parameters.AddWithValue("@Fecha", (object)detalle.FechaDevolucion ?? DBNull.Value);
            if (propia)
                await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            if (propia)
                await cn.DisposeAsync();
        }
    }
}
