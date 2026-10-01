using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class PrestamoNegocio
{
    public const string EstadoPendiente = "Pendiente";
    public const string EstadoDevuelto = "Devuelto";

    private readonly SocioDatos _socios;
    private readonly LibroRepositorio _libros;
    private readonly PrestamoDatos _prestamos;

    public decimal MultaPorDia => ParametrosNegocio.MultaPorDia;
    public int MaxLibrosPendientes => ParametrosNegocio.MaxLibrosPendientes;

    public PrestamoNegocio() : this(new SocioDatos(), new LibroRepositorio(), new PrestamoDatos()) { }

    public PrestamoNegocio(SocioDatos socios, LibroRepositorio libros, PrestamoDatos prestamos)
    {
        _socios = socios ?? throw new ArgumentNullException(nameof(socios));
        _libros = libros ?? throw new ArgumentNullException(nameof(libros));
        _prestamos = prestamos ?? throw new ArgumentNullException(nameof(prestamos));
    }

    public Task<List<Socio>> ListarSociosAsync() => _socios.ListarAsync();

    public Task<List<Libro>> ListarLibrosDisponiblesAsync() => _libros.ListarDisponiblesAsync();

    public Task<int> ContarPendientesAsync(int socioId)
    {
        if (socioId <= 0)
            throw new ReglaNegocioException("Seleccione un socio.");

        return _prestamos.ContarLibrosPendientesAsync(socioId);
    }

    public int DiasRetraso(DateTime fechaLimite, DateTime fechaDevolucion)
    {
        int dias = (fechaDevolucion.Date - fechaLimite.Date).Days;
        return dias > 0 ? dias : 0;
    }

    public decimal CalcularMulta(DateTime fechaLimite, DateTime fechaDevolucion) =>
        DiasRetraso(fechaLimite, fechaDevolucion) * ParametrosNegocio.MultaPorDia;

    public async Task RegistrarAsync(int socioId, DateTime fechaPrestamo, DateTime fechaLimite, IList<int> libroIds)
    {
        if (socioId <= 0)
            throw new ReglaNegocioException("Seleccione un socio.");

        if (fechaPrestamo == default)
            throw new ReglaNegocioException("Indique la fecha de préstamo.");

        if (fechaLimite == default)
            throw new ReglaNegocioException("Indique la fecha límite.");

        if (fechaLimite.Date < fechaPrestamo.Date)
            throw new ReglaNegocioException("La fecha límite no puede ser anterior a la fecha de préstamo.");

        if (libroIds == null || libroIds.Count == 0)
            throw new ReglaNegocioException("Agregue al menos un libro al préstamo.");

        if (libroIds.Distinct().Count() != libroIds.Count)
            throw new ReglaNegocioException("No puede repetir un libro en el mismo préstamo.");

        Socio socio = await _socios.ObtenerPorIdAsync(socioId);
        if (socio == null || !socio.Activo)
            throw new ReglaNegocioException("No se puede prestar a un socio inactivo o que no existe.");

        int pendientes = await _prestamos.ContarLibrosPendientesAsync(socioId);
        if (pendientes + libroIds.Count > ParametrosNegocio.MaxLibrosPendientes)
            throw new ReglaNegocioException(
                $"El socio tiene {pendientes} libro(s) pendiente(s) y este préstamo agrega {libroIds.Count}. " +
                $"El máximo es {ParametrosNegocio.MaxLibrosPendientes}.");

        foreach (int libroId in libroIds)
        {
            Libro libro = await _libros.ObtenerPorIdAsync(libroId);
            if (libro == null || !libro.Activo)
                throw new ReglaNegocioException("No se puede prestar un libro inactivo o que no existe.");

            if (libro.Ejemplares <= 0)
                throw new ReglaNegocioException($"No se puede prestar \"{libro.Titulo}\" porque no tiene ejemplares.");
        }

        var prestamo = new Prestamo
        {
            SocioId = socioId,
            FechaPrestamo = fechaPrestamo.Date,
            FechaLimite = fechaLimite.Date,
            Estado = EstadoPendiente
        };

        try
        {
            await _prestamos.RegistrarAsync(prestamo, libroIds.ToList());
        }
        catch (InvalidOperationException ex)
        {
            throw new ReglaNegocioException(ex.Message);
        }
    }

    public async Task<List<LineaDevolucion>> ListarPendientesAsync(DateTime fechaDevolucion)
    {
        var lista = await _prestamos.ListarPendientesAsync();
        foreach (var linea in lista)
            CompletarMulta(linea, fechaDevolucion);
        return lista;
    }

    public void CompletarMulta(LineaDevolucion linea, DateTime fechaDevolucion)
    {
        linea.DiasRetraso = DiasRetraso(linea.FechaLimite, fechaDevolucion);
        linea.Multa = CalcularMulta(linea.FechaLimite, fechaDevolucion);
        linea.Situacion = linea.DiasRetraso > 0 ? "Atrasado" : "En plazo";
    }

    public async Task<decimal> RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion)
    {
        if (prestamoId <= 0 || libroId <= 0)
            throw new ReglaNegocioException("Seleccione el libro que se devuelve.");

        if (fechaDevolucion == default)
            throw new ReglaNegocioException("Indique la fecha de devolución.");

        LineaDevolucion linea = await _prestamos.ObtenerPendienteAsync(prestamoId, libroId);
        if (linea == null)
            throw new ReglaNegocioException("Ese libro no tiene una devolución pendiente.");

        if (fechaDevolucion.Date < linea.FechaPrestamo.Date)
            throw new ReglaNegocioException("La fecha de devolución no puede ser anterior a la fecha de préstamo.");

        decimal multa = CalcularMulta(linea.FechaLimite, fechaDevolucion);

        try
        {
            await _prestamos.RegistrarDevolucionAsync(prestamoId, libroId, fechaDevolucion.Date, EstadoDevuelto);
        }
        catch (InvalidOperationException ex)
        {
            throw new ReglaNegocioException(ex.Message);
        }

        return multa;
    }

    public async Task<List<PrestamoReporte>> ReporteAsync(DateTime desde, DateTime hasta)
    {
        if (desde == default || hasta == default)
            throw new ReglaNegocioException("Indique el intervalo de fechas.");

        if (desde.Date > hasta.Date)
            throw new ReglaNegocioException("La fecha inicial no puede ser mayor que la fecha final.");

        return await _prestamos.ReporteAsync(desde, hasta);
    }
}
