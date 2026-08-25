namespace MareaBrava.Domain.Entities;

public class Gasto : BaseEntity
{
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaGasto { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }
}