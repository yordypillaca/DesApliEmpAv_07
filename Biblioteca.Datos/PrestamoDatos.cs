using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class PrestamoDatos
{
    private readonly DetallePrestamoDatos _detalles = new DetallePrestamoDatos();
    private readonly LibroRepositorio _libros = new LibroRepositorio();

    public async Task<int> ContarLibrosPendientesAsync(int socioId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM DetallePrestamo d
            INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
            WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@SocioId", socioId);
        await cn.OpenAsync();
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> SocioTienePendientesAsync(int socioId) =>
        await ContarLibrosPendientesAsync(socioId) > 0;

    public async Task RegistrarAsync(Prestamo prestamo, IReadOnlyList<int> libroIds)
    {
        using var cn = DbConnectionFactory.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        try
        {
            int prestamoId = await InsertarCabeceraAsync(prestamo, cn, tx);
            foreach (int libroId in libroIds)
            {
                await _detalles.InsertarAsync(new DetallePrestamo
                {
                    PrestamoId = prestamoId,
                    LibroId = libroId
                }, cn, tx);

                bool desconto = await _libros.DescontarEjemplarAsync(libroId, cn, tx);
                if (!desconto)
                    throw new InvalidOperationException(
                        "No se pudo descontar el ejemplar. El libro no tiene stock o no está activo.");
            }

            tx.Commit();
        }
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    public async Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion, string estadoDevuelto)
    {
        using var cn = DbConnectionFactory.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        try
        {
            bool marcado = await _detalles.MarcarDevueltoAsync(prestamoId, libroId, fechaDevolucion, cn, tx);
            if (!marcado)
                throw new InvalidOperationException("Ese libro ya fue devuelto o no pertenece al préstamo.");

            bool repuso = await _libros.ReponerEjemplarAsync(libroId, cn, tx);
            if (!repuso)
                throw new InvalidOperationException("No se pudo devolver el ejemplar al stock.");

            await CerrarSiNoQuedanPendientesAsync(prestamoId, estadoDevuelto, cn, tx);
            tx.Commit();
        }
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    public async Task<LineaDevolucion> ObtenerPendienteAsync(int prestamoId, int libroId)
    {
        var pendientes = await ListarPendientesAsync();
        return pendientes.FirstOrDefault(p => p.PrestamoId == prestamoId && p.LibroId == libroId);
    }

    public async Task<List<LineaDevolucion>> ListarPendientesAsync()
    {
        const string sql = @"
            SELECT d.PrestamoId, d.LibroId, p.SocioId, s.Nombre, l.Titulo,
                   p.FechaPrestamo, p.FechaLimite, p.Estado
            FROM DetallePrestamo d
            INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            WHERE d.FechaDevolucion IS NULL
            ORDER BY p.FechaLimite, s.Nombre, l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();

        var lista = new List<LineaDevolucion>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new LineaDevolucion
            {
                PrestamoId = dr.GetInt32(0),
                LibroId = dr.GetInt32(1),
                SocioId = dr.GetInt32(2),
                Socio = dr.GetString(3),
                Libro = dr.GetString(4),
                FechaPrestamo = dr.GetDateTime(5),
                FechaLimite = dr.GetDateTime(6),
                Estado = dr.GetString(7)
            });
        }
        return lista;
    }

    public async Task<List<PrestamoReporte>> ReporteAsync(DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT p.PrestamoId, s.Nombre, l.Titulo, p.FechaPrestamo, p.FechaLimite, p.Estado, d.FechaDevolucion
            FROM Prestamos p
            INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            WHERE p.FechaPrestamo >= @Desde AND p.FechaPrestamo <= @Hasta
            ORDER BY p.FechaPrestamo, p.PrestamoId, l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Desde", desde.Date);
        cmd.Parameters.AddWithValue("@Hasta", hasta.Date);
        await cn.OpenAsync();

        var lista = new List<PrestamoReporte>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new PrestamoReporte
            {
                PrestamoId = dr.GetInt32(0),
                Socio = dr.GetString(1),
                Libro = dr.GetString(2),
                FechaPrestamo = dr.GetDateTime(3),
                FechaLimite = dr.GetDateTime(4),
                Estado = dr.GetString(5),
                FechaDevolucion = dr.IsDBNull(6) ? null : dr.GetDateTime(6)
            });
        }
        return lista;
    }

    private static async Task<int> InsertarCabeceraAsync(Prestamo prestamo, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"
            INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
            OUTPUT INSERTED.PrestamoId
            VALUES (@SocioId, @FechaPrestamo, @FechaLimite, @Estado)";

        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@SocioId", prestamo.SocioId);
        cmd.Parameters.AddWithValue("@FechaPrestamo", prestamo.FechaPrestamo.Date);
        cmd.Parameters.AddWithValue("@FechaLimite", prestamo.FechaLimite.Date);
        cmd.Parameters.AddWithValue("@Estado", prestamo.Estado);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task CerrarSiNoQuedanPendientesAsync(int prestamoId, string estadoDevuelto, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"
            UPDATE Prestamos
            SET Estado = @Estado
            WHERE PrestamoId = @Id
              AND NOT EXISTS (
                  SELECT 1 FROM DetallePrestamo
                  WHERE PrestamoId = @Id AND FechaDevolucion IS NULL)";

        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddWithValue("@Estado", estadoDevuelto);
        cmd.Parameters.AddWithValue("@Id", prestamoId);
        await cmd.ExecuteNonQueryAsync();
    }
}
