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

    public async Task<IEnumerable<Prenda>> ObtenerTodosAsync()
    {
        return await _context.Prendas
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Prenda?> ObtenerPorIdAsync(int id)
    {
        return await _context.Prendas.FindAsync(id);
    }

    public async Task<Prenda?> ObtenerPorSkuAsync(string sku)
    {
        return await _context.Prendas
            .FirstOrDefaultAsync(p => p.Sku.ToLower() == sku.ToLower());
    }

    public async Task<IEnumerable<Prenda>> ObtenerBajoStockAsync()
    {
        return await _context.Prendas
            .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
            .ToListAsync();
    }

    public async Task<Prenda> AgregarAsync(Prenda prenda)
    {
        await _context.Prendas.AddAsync(prenda);
        await _context.SaveChangesAsync();
        return prenda;
    }

    public async Task ActualizarAsync(Prenda prenda)
    {
        _context.Prendas.Update(prenda);
        await _context.SaveChangesAsync();
    }
}