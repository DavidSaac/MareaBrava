using MareaBrava.Application.DTOs;

namespace MareaBrava.Application.Interfaces;

public interface IVentaService
{
    Task<IEnumerable<VentaDto>> ObtenerHistorialVentasAsync();
    Task<VentaDto?> ObtenerPorIdAsync(int id);
    Task<VentaDto?> ObtenerPorTicketAsync(string numeroTicket);
    Task<IEnumerable<VentaDto>> ObtenerVentasPorFechaAsync(DateTime fechaInicio, DateTime fechaFin);
    Task<VentaDto> ProcesarVentaAsync(RegistrarVentaDto dto);
}