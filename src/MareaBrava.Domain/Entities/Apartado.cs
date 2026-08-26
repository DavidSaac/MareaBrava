namespace MareaBrava.Domain.Entities;

public class Apartado : BaseEntity
{
    public string NumeroApartado { get; set; } = string.Empty;
    public string NombreClienta { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public decimal Anticipo { get; set; }
    public decimal Total { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaLiquidacion { get; set; }
    public string Estado { get; set; } = "Activo";
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public List<DetalleApartado> Detalles { get; set; } = new();
}