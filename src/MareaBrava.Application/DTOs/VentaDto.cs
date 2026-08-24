using System.ComponentModel.DataAnnotations;
using MareaBrava.Domain.Enums;

namespace MareaBrava.Application.DTOs;

public class VentaDto
{
    public int Id { get; set; }
    public string NumeroTicket { get; set; } = string.Empty;
    public DateTime FechaVenta { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public decimal Total { get; set; }
    public int UsuarioId { get; set; }
    public List<DetalleVentaDto> Detalles { get; set; } = new();
}

public class DetalleVentaDto
{
    public int PrendaId { get; set; }
    public string NombrePrenda { get; set; } = string.Empty;
    public string SkuPrenda { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class RegistrarVentaDto
{
    [Required(ErrorMessage = "El ID de usuario es obligatorio")]
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "Debe indicar un método de pago")]
    public MetodoPago MetodoPago { get; set; }

    [MinLength(1, ErrorMessage = "La venta debe incluir al menos una prenda")]
    public List<RegistrarDetalleDto> Lineas { get; set; } = new();
}

public class RegistrarDetalleDto
{
    [Required]
    public int PrendaId { get; set; }

    [Range(1, 1000, ErrorMessage = "La cantidad debe ser al menos 1")]
    public int Cantidad { get; set; }
}