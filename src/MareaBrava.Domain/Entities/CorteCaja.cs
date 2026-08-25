namespace MareaBrava.Domain.Entities;

public class CorteCaja
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    
    public decimal FondoInicial { get; set; }
    public decimal VentasEfectivo { get; set; }
    public decimal VentasTarjeta { get; set; }
    public decimal VentasTransferencia { get; set; }
    public decimal TotalEsperadoEfectivo { get; set; }
    public decimal EfectivoRealContado { get; set; }
    public decimal Diferencia { get; set; }
    
    public bool Abierto { get; set; } = true;
    public string? Observaciones { get; set; }
}