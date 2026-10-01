using System.Configuration;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

internal static class DbConnectionFactory
{
    private static string ConnectionString =>
        ConfigurationManager.ConnectionStrings["DB"]?.ConnectionString
        ?? throw new InvalidOperationException(
            "No se encontró la cadena de conexión 'DB' en el App.config del proyecto de inicio.");

    public static SqlConnection Create() => new SqlConnection(ConnectionString);

    public static string Contiene(string texto)
    {
        string safe = (texto ?? string.Empty)
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
        return "%" + safe + "%";
    }
}
