using Microsoft.EntityFrameworkCore;
using MareaBrava.Application.Interfaces;
using MareaBrava.Domain.Entities;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.Infrastructure.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly MareaBravaDbContext _context;

    public VentaRepository(MareaBravaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Venta>> ObtenerTodasAsync()
    {
        return await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Prenda)
            .OrderByDescending(v => v.FechaVenta)
            .ToListAsync();
    }

    public async Task<Venta?> ObtenerPorIdAsync(int id)
    {
        return await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Prenda)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Venta?> ObtenerPorTicketAsync(string numeroTicket)
    {
        return await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Prenda)
            .FirstOrDefaultAsync(v => v.NumeroTicket == numeroTicket);
    }

    public async Task<IEnumerable<Venta>> ObtenerPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        return await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Prenda)
            .Where(v => v.FechaVenta >= fechaInicio && v.FechaVenta <= fechaFin)
            .OrderByDescending(v => v.FechaVenta)
            .ToListAsync();
    }

    public async Task<int> ObtenerConteoVentasHoyAsync()
    {
        var hoy = DateTime.UtcNow.Date;
        var manana = hoy.AddDays(1);
        
        return await _context.Ventas
            .CountAsync(v => v.FechaVenta >= hoy && v.FechaVenta < manana);
    }

    public async Task<Venta> RegistrarVentaAsync(Venta venta)
    {
        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();
        return venta;
    }
}