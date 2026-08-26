using MareaBrava.Domain.Enums;

namespace MareaBrava.Domain.Entities;

public class Venta : BaseEntity
{
    public string NumeroTicket { get; set; } = string.Empty;
    public DateTime FechaVenta { get; set; } = DateTime.UtcNow;
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
    public decimal Total { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal DescuentoMonto { get; set; }
    public decimal EfectivoRecibido { get; set; }
    public decimal Cambio { get; set; }
    public bool EsVentaDesarmada { get; set; }
    public string? NotaAdministrativa { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public List<DetalleVenta> Detalles { get; set; } = new();
}