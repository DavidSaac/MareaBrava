using Microsoft.EntityFrameworkCore;
using MareaBrava.Application.Interfaces;
using MareaBrava.Domain.Entities;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.Infrastructure.Repositories;

public class PrendaRepository : IPrendaRepository
{
    private readonly MareaBravaDbContext _context;

    public PrendaRepository(MareaBravaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Prenda>> ObtenerTodasAsync()
    {
        return await _context.Prendas
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Prenda?> ObtenerPorIdAsync(int id)
    {
        return await _context.Prendas
            .FirstOrDefaultAsync(p => p.Id == id && p.Activo);
    }

    public async Task<Prenda?> ObtenerPorSkuAsync(string sku)
    {
        return await _context.Prendas
            .FirstOrDefaultAsync(p => p.Sku.ToLower() == sku.ToLower() && p.Activo);
    }

    public async Task<IEnumerable<Prenda>> ObtenerBajoStockAsync()
    {
        return await _context.Prendas
            .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
            .OrderBy(p => p.StockActual)
            .ToListAsync();
    }

    public async Task<Prenda> AgregarAsync(Prenda prenda)
    {
        _context.Prendas.Add(prenda);
        await _context.SaveChangesAsync();
        return prenda;
    }

    public async Task ActualizarAsync(Prenda prenda)
    {
        prenda.FechaModificacion = DateTime.UtcNow;
        _context.Prendas.Update(prenda);
        await _context.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        var prenda = await ObtenerPorIdAsync(id);
        if (prenda != null)
        {
            prenda.Activo = false; // Eliminación lógica
            prenda.FechaModificacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}