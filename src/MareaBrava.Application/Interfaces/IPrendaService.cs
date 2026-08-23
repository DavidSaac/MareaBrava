using MareaBrava.Application.DTOs;

namespace MareaBrava.Application.Interfaces;

public interface IPrendaService
{
    Task<IEnumerable<PrendaDto>> ObtenerCatalogoAsync();
    Task<PrendaDto?> ObtenerPorIdAsync(int id);
    Task<PrendaDto?> ObtenerPorSkuAsync(string sku);
    Task<IEnumerable<PrendaDto>> ObtenerPrendasBajoStockAsync();
    Task<PrendaDto> CrearPrendaAsync(CrearPrendaDto dto);
    Task ActualizarPrendaAsync(int id, CrearPrendaDto dto);
    Task EliminarPrendaAsync(int id);
}
