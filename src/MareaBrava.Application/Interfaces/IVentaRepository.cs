using MareaBrava.Domain.Entities;

namespace MareaBrava.Application.Interfaces;

public interface IVentaRepository
{
    Task<IEnumerable<Venta>> ObtenerTodasAsync();
    Task<Venta?> ObtenerPorIdAsync(int id);
    Task<Venta?> ObtenerPorTicketAsync(string numeroTicket);
    Task<IEnumerable<Venta>> ObtenerPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);
    Task<int> ObtenerConteoVentasHoyAsync();
    Task<Venta> RegistrarVentaAsync(Venta venta);
}