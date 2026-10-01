using System.Configuration;
using System.Globalization;

namespace Biblioteca.Negocio;

public static class ParametrosNegocio
{
    public const decimal MultaPorDiaPredeterminada = 1.50m;
    public const int MaxLibrosPendientesPredeterminado = 3;

    public static decimal MultaPorDia { get; } = LeerDecimal("MultaPorDia", MultaPorDiaPredeterminada);
    public static int MaxLibrosPendientes { get; } = LeerEntero("MaxLibrosPendientes", MaxLibrosPendientesPredeterminado);

    private static decimal LeerDecimal(string clave, decimal valorPredeterminado)
    {
        string valor = ConfigurationManager.AppSettings[clave];
        return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var resultado)
            ? resultado
            : valorPredeterminado;
    }

    private static int LeerEntero(string clave, int valorPredeterminado)
    {
        string valor = ConfigurationManager.AppSettings[clave];
        return int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resultado)
            ? resultado
            : valorPredeterminado;
    }
}
