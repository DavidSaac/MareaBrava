namespace MareaBrava.Domain.Entities;

public class DetalleVenta : BaseEntity
{
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int? PrendaId { get; set; }
    public Prenda? Prenda { get; set; }

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? Descripcion { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
}