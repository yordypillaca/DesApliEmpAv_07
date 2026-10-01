namespace Biblioteca.Entidades;

public class PrestamoReporte
{
    public int PrestamoId { get; set; }
    public string Socio { get; set; }
    public string Libro { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public string Estado { get; set; }
    public DateTime? FechaDevolucion { get; set; }
}
