using MareaBrava.Domain.Entities;

namespace MareaBrava.Application.Interfaces;

public interface IPrendaRepository
{
    Task<IEnumerable<Prenda>> ObtenerTodosAsync();
    Task<Prenda?> ObtenerPorIdAsync(int id);
    Task<Prenda?> ObtenerPorSkuAsync(string sku);
    Task<IEnumerable<Prenda>> ObtenerBajoStockAsync();
    Task<Prenda> AgregarAsync(Prenda prenda);
    Task ActualizarAsync(Prenda prenda);
}