namespace MareaBrava.Domain.Entities;

public class DetalleApartado : BaseEntity
{
    public int ApartadoId { get; set; }
    public Apartado? Apartado { get; set; }
    public int PrendaId { get; set; }
    public Prenda? Prenda { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}