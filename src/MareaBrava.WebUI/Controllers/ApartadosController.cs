using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Cajero")]
public class ApartadosController : ControllerBase
{
    private readonly MareaBravaDbContext _context;

    public ApartadosController(MareaBravaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        await ExpirarVencidosAsync();
        var apartados = await _context.Apartados
            .Include(a => a.Detalles).ThenInclude(d => d.Prenda)
            .Where(a => a.Activo && a.Estado == "Activo")
            .OrderBy(a => a.FechaExpiracion)
            .Select(a => new
            {
                a.Id,
                a.NumeroApartado,
                a.NombreClienta,
                a.Telefono,
                a.Anticipo,
                a.Total,
                a.FechaCreacion,
                a.FechaExpiracion,
                a.Estado,
                requiereAviso24h = a.FechaCreacion.AddHours(24) <= DateTime.UtcNow,
                Detalles = a.Detalles.Select(d => new { d.PrendaId, d.Cantidad, d.PrecioUnitario, Nombre = d.Prenda!.Nombre })
            })
            .ToListAsync();
        return Ok(apartados);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearApartadoRequest request)
    {
        await ExpirarVencidosAsync();
        if (string.IsNullOrWhiteSpace(request.NombreClienta) || string.IsNullOrWhiteSpace(request.Telefono) || request.Anticipo <= 0)
            return BadRequest(new { error = "Nombre, teléfono y anticipo son obligatorios." });
        if (request.Lineas == null || request.Lineas.Count == 0 || request.Lineas.Any(l => l.Cantidad <= 0))
            return BadRequest(new { error = "El apartado debe contener artículos con cantidades válidas." });
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return Unauthorized();

        var ids = request.Lineas.Select(l => l.PrendaId).Distinct().ToList();
        var prendas = await _context.Prendas.Where(p => ids.Contains(p.Id) && p.Activo).ToListAsync();
        var detalles = new List<DetalleApartado>();
        decimal total = 0;
        foreach (var linea in request.Lineas)
        {
            var prenda = prendas.SingleOrDefault(p => p.Id == linea.PrendaId);
            if (prenda == null) return BadRequest(new { error = $"La prenda {linea.PrendaId} no existe." });
            if (prenda.StockActual < linea.Cantidad) return BadRequest(new { error = $"Stock insuficiente para '{prenda.Nombre}'." });
            prenda.StockActual -= linea.Cantidad;
            detalles.Add(new DetalleApartado { PrendaId = prenda.Id, Cantidad = linea.Cantidad, PrecioUnitario = prenda.PrecioVenta });
            total += prenda.PrecioVenta * linea.Cantidad;
        }
        if (request.Anticipo > total) return BadRequest(new { error = "El anticipo no puede superar el total." });

        var apartado = new Apartado
        {
            NumeroApartado = $"APT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            NombreClienta = request.NombreClienta.Trim(),
            Telefono = request.Telefono.Trim(),
            Anticipo = request.Anticipo,
            Total = total,
            FechaExpiracion = DateTime.UtcNow.AddHours(48),
            UsuarioId = usuarioId,
            Detalles = detalles,
            Activo = true,
            Estado = "Activo"
        };
        _context.Apartados.Add(apartado);
        await _context.SaveChangesAsync();
        return Ok(new { apartado.Id, apartado.NumeroApartado, apartado.NombreClienta, apartado.Telefono, apartado.Anticipo, apartado.Total, apartado.FechaCreacion, apartado.FechaExpiracion, saldo = total - request.Anticipo });
    }

    [HttpPost("{id}/liquidar")]
    public async Task<IActionResult> Liquidar(int id)
    {
        var apartado = await _context.Apartados.FirstOrDefaultAsync(a => a.Id == id && a.Activo && a.Estado == "Activo");
        if (apartado == null) return NotFound(new { error = "Apartado no encontrado o expirado." });
        if (apartado.FechaExpiracion <= DateTime.UtcNow) { await ExpirarVencidosAsync(); return BadRequest(new { error = "El apartado ya expiró." }); }
        apartado.Estado = "Liquidado";
        apartado.FechaLiquidacion = DateTime.UtcNow;
        apartado.FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { apartado.NumeroApartado, mensaje = "Apartado liquidado." });
    }

    private async Task ExpirarVencidosAsync()
    {
        var vencidos = await _context.Apartados
            .Include(a => a.Detalles)
            .Where(a => a.Activo && a.Estado == "Activo" && a.FechaExpiracion <= DateTime.UtcNow)
            .ToListAsync();
        if (vencidos.Count == 0) return;
        foreach (var apartado in vencidos)
        {
            foreach (var detalle in apartado.Detalles)
            {
                var prenda = await _context.Prendas.FindAsync(detalle.PrendaId);
                if (prenda != null) prenda.StockActual += detalle.Cantidad;
            }
            apartado.Estado = "Expirado";
            apartado.Activo = false;
            apartado.FechaModificacion = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}

public record CrearApartadoRequest(string? NombreClienta, string? Telefono, decimal Anticipo, List<LineaApartadoRequest> Lineas);
public record LineaApartadoRequest(int PrendaId, int Cantidad);
