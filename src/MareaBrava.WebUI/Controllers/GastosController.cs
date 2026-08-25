using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class GastosController : ControllerBase
{
    private readonly MareaBravaDbContext _context;

    public GastosController(MareaBravaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _context.Gastos.Where(g => g.Activo).AsQueryable();
        if (desde.HasValue) query = query.Where(g => g.FechaGasto >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(g => g.FechaGasto < hasta.Value.Date.AddDays(1));

        return Ok(await query.OrderByDescending(g => g.FechaGasto)
            .Select(g => new { g.Id, g.Concepto, g.Monto, g.FechaGasto, g.Observaciones })
            .ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GastoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Concepto) || request.Concepto.Trim().Length > 150 || request.Monto <= 0)
            return BadRequest(new { error = "Indica un concepto y un monto mayor que cero." });

        var gasto = new Gasto
        {
            Concepto = request.Concepto.Trim(),
            Monto = request.Monto,
            FechaGasto = request.Fecha?.Date ?? DateTime.UtcNow.Date,
            Observaciones = request.Observaciones?.Trim()
        };
        _context.Gastos.Add(gasto);
        await _context.SaveChangesAsync();
        return Ok(gasto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var gasto = await _context.Gastos.FindAsync(id);
        if (gasto == null || !gasto.Activo) return NotFound();
        gasto.Activo = false;
        gasto.FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record GastoRequest(string? Concepto, decimal Monto, DateTime? Fecha, string? Observaciones);