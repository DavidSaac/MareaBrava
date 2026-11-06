using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class CortesController : ControllerBase
{
    private static readonly TimeZoneInfo ZonaHorariaMexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
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
            totalPrendasVendidas = await _context.DetallesVentas
                .Where(d => d.Venta != null && d.Venta.FechaCreacion >= corteAbierto.FechaApertura && d.Venta.Activo)
                .SumAsync(d => (int?)d.Cantidad) ?? 0
        });
    }

    // Abrir turno con Fondo Inicial
    [HttpPost("abrir")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador,Cajero")]
    public async Task<IActionResult> AbrirTurno([FromBody] AbrirTurnoRequest req)
    {
        if (req.FondoInicial < 0)
            return BadRequest(new { error = "El fondo inicial no puede ser negativo." });

        var existeAbierto = await _context.CortesCaja.AnyAsync(c => c.Abierto);
        if (existeAbierto)
            return BadRequest(new { error = "Ya existe un turno de caja abierto en el sistema." });

        var nuevoCorte = new CorteCaja
        {
            UsuarioId = ObtenerUsuarioId(),
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
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador,Cajero")]
    public async Task<IActionResult> CerrarTurno([FromBody] CerrarTurnoRequest req)
    {
        var corte = await _context.CortesCaja
            .FirstOrDefaultAsync(c => c.Id == req.CorteId && c.Abierto);
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

    private int ObtenerUsuarioId()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            throw new InvalidOperationException("La sesión no contiene un usuario válido.");

        return usuarioId;
    }

    // Historial de Cortes (para la Dueña)
    [HttpGet("historial")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ObtenerHistorial([FromQuery] DateOnly? fecha, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] bool todo = false)
    {
        IQueryable<CorteCaja> query = _context.CortesCaja
            .Include(c => c.Usuario)
            .OrderByDescending(c => c.FechaApertura);

        if (!fecha.HasValue && !desde.HasValue && !hasta.HasValue && !todo)
        {
            var ultimosCortes = await query.Take(30).ToListAsync();
            return Ok(ultimosCortes);
        }

        if (todo)
        {
            var todosLosCortes = await query.ToListAsync();
            return Ok(ProyectarHistorial(todosLosCortes));
        }

        DateTime inicioUtc;
        DateTime finUtc;

        if (fecha.HasValue)
        {
            inicioUtc = InicioDiaUtc(fecha.Value);
            finUtc = InicioDiaUtc(fecha.Value.AddDays(1));
        }
        else if (desde.HasValue && hasta.HasValue)
        {
            inicioUtc = InicioDiaUtc(desde.Value);
            finUtc = InicioDiaUtc(hasta.Value.AddDays(1));
        }
        else if (desde.HasValue)
        {
            inicioUtc = InicioDiaUtc(desde.Value);
            finUtc = DateTime.MaxValue;
        }
        else
        {
            inicioUtc = DateTime.MinValue;
            finUtc = InicioDiaUtc(hasta!.Value.AddDays(1));
        }

        var cortes = await query
            .Where(c => c.FechaApertura >= inicioUtc && c.FechaApertura < finUtc)
            .ToListAsync();
        return Ok(ProyectarHistorial(cortes));
    }

    private DateTime InicioDiaUtc(DateOnly fecha)
    {
        var local = DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, ZonaHorariaMexico);
    }

    private IEnumerable<object> ProyectarHistorial(IEnumerable<CorteCaja> cortes)
    {
        return cortes.Select(c =>
        {
            var fechaAperturaUtc = DateTime.SpecifyKind(c.FechaApertura, DateTimeKind.Utc);
            var fechaCierreUtc = c.FechaCierre.HasValue
                ? DateTime.SpecifyKind(c.FechaCierre.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            return new
            {
                c.Id,
                fechaOperacion = TimeZoneInfo.ConvertTimeFromUtc(fechaAperturaUtc, ZonaHorariaMexico).ToString("yyyy-MM-dd"),
                fechaApertura = fechaAperturaUtc,
                usuarioApertura = c.Usuario.NombreCompleto,
                usuarioCierre = (string?)null,
                fechaCierre = fechaCierreUtc,
                c.FondoInicial,
                c.VentasEfectivo,
                c.VentasTarjeta,
                c.VentasTransferencia,
                c.TotalEsperadoEfectivo,
                c.EfectivoRealContado,
                c.Diferencia,
                c.Abierto,
                observaciones = c.Observaciones
            };
        });
    }
}

public record AbrirTurnoRequest(int UsuarioId, decimal FondoInicial);
public record CerrarTurnoRequest(int CorteId, decimal EfectivoRealContado, string? Observaciones);