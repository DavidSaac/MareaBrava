using MareaBrava.Domain.Enums;

namespace MareaBrava.Domain.Entities;

public class Prenda : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoPieza TipoPieza { get; set; }
    public int? CategoriaId { get; set; }
    public CategoriaProducto? Categoria { get; set; }
    public TallaPrenda Talla { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; } = 3;
    public string? ImagenUrl { get; set; }
}