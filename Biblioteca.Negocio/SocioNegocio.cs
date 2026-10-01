using System.Net.Mail;
using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class SocioNegocio
{
    private readonly SocioDatos _socios;
    private readonly PrestamoDatos _prestamos;

    public SocioNegocio() : this(new SocioDatos(), new PrestamoDatos()) { }

    public SocioNegocio(SocioDatos socios, PrestamoDatos prestamos)
    {
        _socios = socios ?? throw new ArgumentNullException(nameof(socios));
        _prestamos = prestamos ?? throw new ArgumentNullException(nameof(prestamos));
    }

    public Task<List<Socio>> ListarAsync() => _socios.ListarAsync();

    public Task<List<Socio>> BuscarAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? ListarAsync()
            : _socios.BuscarAsync(texto.Trim());

    public async Task RegistrarAsync(Socio socio)
    {
        await ValidarAsync(socio);
        await _socios.InsertarAsync(socio);
    }

    public async Task ActualizarAsync(Socio socio)
    {
        if (socio.SocioId <= 0)
            throw new ReglaNegocioException("Seleccione un socio para actualizar.");

        await ValidarAsync(socio);
        await _socios.ActualizarAsync(socio);
    }

    public async Task EliminarAsync(int socioId)
    {
        if (socioId <= 0)
            throw new ReglaNegocioException("Seleccione un socio.");

        if (await _prestamos.SocioTienePendientesAsync(socioId))
            throw new ReglaNegocioException("No se puede dar de baja un socio con préstamos pendientes.");

        await _socios.EliminarLogicoAsync(socioId);
    }

    private async Task ValidarAsync(Socio socio)
    {
        socio.DNI = socio.DNI?.Trim();
        socio.Nombre = socio.Nombre?.Trim();
        socio.Email = string.IsNullOrWhiteSpace(socio.Email) ? null : socio.Email.Trim();

        if (string.IsNullOrWhiteSpace(socio.DNI))
            throw new ReglaNegocioException("El DNI es obligatorio.");

        if (socio.DNI.Length != 8 || !socio.DNI.All(char.IsDigit))
            throw new ReglaNegocioException("El DNI debe tener 8 dígitos.");

        if (string.IsNullOrWhiteSpace(socio.Nombre))
            throw new ReglaNegocioException("El nombre es obligatorio.");

        if (socio.Nombre.Length > 120)
            throw new ReglaNegocioException("El nombre no puede superar 120 caracteres.");

        if (socio.Email != null && socio.Email.Length > 120)
            throw new ReglaNegocioException("El correo no puede superar 120 caracteres.");

        if (socio.Email != null && !EsCorreoValido(socio.Email))
            throw new ReglaNegocioException("El formato del correo no es válido.");

        if (await _socios.ExisteDniAsync(socio.DNI, socio.SocioId))
            throw new ReglaNegocioException($"El DNI '{socio.DNI}' ya está registrado.");
    }

    private static bool EsCorreoValido(string email) =>
        MailAddress.TryCreate(email, out var direccion) && direccion.Address == email;
}
