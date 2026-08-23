using System.ComponentModel.DataAnnotations;
using MareaBrava.Domain.Enums;

namespace MareaBrava.Application.DTOs;

public class PrendaDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoPieza TipoPieza { get; set; }
    public TallaPrenda Talla { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public string? ImagenUrl { get; set; }
}

public class CrearPrendaDto
{
    [Required(ErrorMessage = "El código SKU es obligatorio")]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la prenda es obligatorio")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un tipo de pieza")]
    public TipoPieza TipoPieza { get; set; }

    [Required(ErrorMessage = "Debe seleccionar una talla")]
    public TallaPrenda Talla { get; set; }

    [Required(ErrorMessage = "El color es obligatorio")]
    public string Color { get; set; } = string.Empty;

    [Range(0.01, 100000, ErrorMessage = "El precio de costo debe ser mayor a 0")]
    public decimal PrecioCosto { get; set; }

    [Range(0.01, 100000, ErrorMessage = "El precio de venta debe ser mayor a 0")]
    public decimal PrecioVenta { get; set; }

    [Range(0, 10000, ErrorMessage = "El stock no puede ser negativo")]
    public int StockActual { get; set; }

    [Range(1, 1000, ErrorMessage = "El stock mínimo debe ser al menos 1")]
    public int StockMinimo { get; set; } = 3;

    public string? ImagenUrl { get; set; }
}