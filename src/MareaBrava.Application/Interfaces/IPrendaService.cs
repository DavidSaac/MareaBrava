using MareaBrava.Application.DTOs;
using MareaBrava.Domain.Entities;

namespace MareaBrava.Application.Interfaces;

public interface IPrendaService
{
    Task<IEnumerable<Prenda>> ObtenerCatalogoActivoAsync();
    Task<Prenda?> ObtenerPorIdAsync(int id);
    Task<IEnumerable<Prenda>> ObtenerAlertasBajoStockAsync();
    Task<Prenda> RegistrarNuevaPrendaAsync(CrearPrendaDto dto);
    Task<Prenda> ActualizarPrendaAsync(int id, CrearPrendaDto dto);
    Task<Prenda> AjustarStockAsync(int id, int cambioStock);
    Task<bool> DarDeBajaPrendaAsync(int id);
}