using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CortesController : ControllerBase
{
    private readonly MareaBravaDbContext _context;

    public CortesController(MareaBravaDbContext context)
    {
        _context = context;
    }

    // Obtener estado actual de la caja (si hay un turno abierto)
    [HttpGet("estado-actual")]
    public async Task<IActionResult> ObtenerEstadoActual([FromQuery] int usuarioId)
    {
        var corteAbierto = await _context.CortesCaja
            .Include(c => c.Usuario)
            .Where(c => c.Abierto)
            .OrderByDescending(c => c.FechaApertura)
            .FirstOrDefaultAsync();

        if (corteAbierto == null)
        {
            return Ok(new { tieneTurnoAbierto = false });
        }

        // Calcular ventas realizadas desde la apertura de este turno
        var ventas = await _context.Ventas
            .Where(v => v.FechaCreacion >= corteAbierto.FechaApertura && v.Activo)
            .ToListAsync();

        var ventasEfectivo = ventas.Where(v => v.MetodoPago == MetodoPago.Efectivo).Sum(v => v.Total);
        var ventasTarjeta = ventas.Where(v => v.MetodoPago == MetodoPago.Tarjeta).Sum(v => v.Total);
        var ventasTransf = ventas.Where(v => v.MetodoPago == MetodoPago.Transferencia).Sum(v => v.Total);
        var totalEsperadoEfectivo = corteAbierto.FondoInicial + ventasEfectivo;

        return Ok(new
        {
            tieneTurnoAbierto = true,
            corteId = corteAbierto.Id,
            cajero = corteAbierto.Usuario.NombreCompleto,
            fechaApertura = corteAbierto.FechaApertura,
            fondoInicial = corteAbierto.FondoInicial,
            ventasEfectivo,
            ventasTarjeta,
            ventasTransf,
            totalEsperadoEfectivo,
            totalVentasTurno = ventasEfectivo + ventasTarjeta + ventasTransf,
            totalPrendasVendidas = ventas.Count
        });
    }

    // Abrir turno con Fondo Inicial
    [HttpPost("abrir")]
    public async Task<IActionResult> AbrirTurno([FromBody] AbrirTurnoRequest req)
    {
        var existeAbierto = await _context.CortesCaja.AnyAsync(c => c.Abierto);
        if (existeAbierto)
            return BadRequest(new { error = "Ya existe un turno de caja abierto en el sistema." });

        var nuevoCorte = new CorteCaja
        {
            UsuarioId = req.UsuarioId,
            FondoInicial = req.FondoInicial,
            FechaApertura = DateTime.UtcNow,
            Abierto = true
        };

        _context.CortesCaja.Add(nuevoCorte);
        await _context.SaveChangesAsync();

        return Ok(nuevoCorte);
    }

    // Cerrar turno con Arqueo de Dinero
    [HttpPost("cerrar")]
    public async Task<IActionResult> CerrarTurno([FromBody] CerrarTurnoRequest req)
    {
        var corte = await _context.CortesCaja.FirstOrDefaultAsync(c => c.Id == req.CorteId && c.Abierto);
        if (corte == null)
            return BadRequest(new { error = "No se encontró un turno activo con este identificador." });

        var ventas = await _context.Ventas
            .Where(v => v.FechaCreacion >= corte.FechaApertura && v.Activo)
            .ToListAsync();

        corte.VentasEfectivo = ventas.Where(v => v.MetodoPago == MetodoPago.Efectivo).Sum(v => v.Total);
        corte.VentasTarjeta = ventas.Where(v => v.MetodoPago == MetodoPago.Tarjeta).Sum(v => v.Total);
        corte.VentasTransferencia = ventas.Where(v => v.MetodoPago == MetodoPago.Transferencia).Sum(v => v.Total);
        
        corte.TotalEsperadoEfectivo = corte.FondoInicial + corte.VentasEfectivo;
        corte.EfectivoRealContado = req.EfectivoRealContado;
        corte.Diferencia = corte.EfectivoRealContado - corte.TotalEsperadoEfectivo;
        
        corte.FechaCierre = DateTime.UtcNow;
        corte.Abierto = false;
        corte.Observaciones = req.Observaciones;

        await _context.SaveChangesAsync();

        return Ok(corte);
    }

    // Historial de Cortes (para la Dueña)
    [HttpGet("historial")]
    public async Task<IActionResult> ObtenerHistorial()
    {
        var cortes = await _context.CortesCaja
            .Include(c => c.Usuario)
            .OrderByDescending(c => c.FechaApertura)
            .Take(30)
            .ToListAsync();

        return Ok(cortes);
    }
}

public record AbrirTurnoRequest(int UsuarioId, decimal FondoInicial);
public record CerrarTurnoRequest(int CorteId, decimal EfectivoRealContado, string? Observaciones);