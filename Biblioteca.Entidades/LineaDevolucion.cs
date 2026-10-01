namespace Biblioteca.Entidades;

public class LineaDevolucion
{
    public int PrestamoId { get; set; }
    public int LibroId { get; set; }
    public int SocioId { get; set; }
    public string Socio { get; set; }
    public string Libro { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public string Estado { get; set; }
    public string Situacion { get; set; }
    public int DiasRetraso { get; set; }
    public decimal Multa { get; set; }
}
