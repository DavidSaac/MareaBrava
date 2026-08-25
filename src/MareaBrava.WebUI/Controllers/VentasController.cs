using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class VentasController : ControllerBase
{
    private readonly MareaBravaDbContext _context;

    public VentasController(MareaBravaDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CrearVenta([FromBody] CrearVentaDto dto)
    {
        if (dto.Lineas == null || !dto.Lineas.Any())
            return BadRequest(new { error = "El ticket no contiene prendas." });

        if (dto.Lineas.Any(l => l.Cantidad <= 0))
            return BadRequest(new { error = "La cantidad de cada prenda debe ser mayor que cero." });

        if (!Enum.IsDefined(typeof(MetodoPago), dto.MetodoPago))
            return BadRequest(new { error = "El método de pago no es válido." });

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var prendaIds = dto.Lineas.Select(l => l.PrendaId).ToList();
        var prendas = await _context.Prendas.Where(p => prendaIds.Contains(p.Id)).ToListAsync();

        decimal totalVenta = 0;
        var detalles = new List<DetalleVenta>();

        foreach (var linea in dto.Lineas)
        {
            var prenda = prendas.FirstOrDefault(p => p.Id == linea.PrendaId);
            if (prenda == null || !prenda.Activo)
                return BadRequest(new { error = $"La prenda con ID {linea.PrendaId} no existe o está inactiva." });

            if (prenda.StockActual < linea.Cantidad)
                return BadRequest(new { error = $"Stock insuficiente para '{prenda.Nombre}'. Disponibles: {prenda.StockActual}" });

            prenda.StockActual -= linea.Cantidad;

            var detalle = new DetalleVenta
            {
                PrendaId = prenda.Id,
                Cantidad = linea.Cantidad,
                PrecioUnitario = prenda.PrecioVenta
            };

            totalVenta += detalle.PrecioUnitario * detalle.Cantidad;
            detalles.Add(detalle);
        }

        var consecutivo = await _context.Ventas.CountAsync() + 1;
        var numeroTicket = $"MB-{DateTime.UtcNow:yyyyMMdd}-{consecutivo:D4}";

        var venta = new Venta
        {
            NumeroTicket = numeroTicket,
            UsuarioId = usuarioId,
            MetodoPago = (MetodoPago)dto.MetodoPago,
            Total = totalVenta,
            Detalles = detalles,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            venta.Id,
            venta.NumeroTicket,
            venta.Total,
            venta.FechaCreacion,
            venta.MetodoPago
        });
    }

    [HttpPost("sincronizar-lote")]
    public async Task<IActionResult> SincronizarLote([FromBody] List<VentaOfflineDto> ventasOffline)
    {
        if (ventasOffline == null || !ventasOffline.Any())
            return Ok(new { procesadas = 0, mensaje = "No hay ventas para sincronizar." });

        int procesadas = 0;

        foreach (var vOff in ventasOffline)
        {
            if (vOff.Lineas == null || vOff.Lineas.Count == 0 || vOff.Lineas.Any(l => l.Cantidad <= 0))
                return BadRequest(new { error = "Cada venta offline debe contener cantidades mayores que cero." });

            if (!Enum.IsDefined(typeof(MetodoPago), vOff.MetodoPago))
                return BadRequest(new { error = "El método de pago no es válido." });

            var prendaIds = vOff.Lineas.Select(l => l.PrendaId).ToList();
            var prendas = await _context.Prendas.Where(p => prendaIds.Contains(p.Id)).ToListAsync();

            decimal totalVenta = 0;
            var detalles = new List<DetalleVenta>();

            foreach (var linea in vOff.Lineas)
            {
                var prenda = prendas.FirstOrDefault(p => p.Id == linea.PrendaId);
                if (prenda != null && prenda.Activo)
                {
                    if (prenda.StockActual < linea.Cantidad)
                        return BadRequest(new { error = $"Stock insuficiente para '{prenda.Nombre}'." });

                    prenda.StockActual -= linea.Cantidad;

                    var detalle = new DetalleVenta
                    {
                        PrendaId = prenda.Id,
                        Cantidad = linea.Cantidad,
                        PrecioUnitario = prenda.PrecioVenta
                    };

                    totalVenta += detalle.PrecioUnitario * detalle.Cantidad;
                    detalles.Add(detalle);
                }
            }

            var consecutivo = await _context.Ventas.CountAsync() + 1;
            var numeroTicket = $"MB-{DateTime.UtcNow:yyyyMMdd}-{consecutivo:D4}";

            var venta = new Venta
            {
                NumeroTicket = numeroTicket,
                UsuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                MetodoPago = (MetodoPago)vOff.MetodoPago,
                Total = totalVenta > 0 ? totalVenta : vOff.TotalEstimado,
                Detalles = detalles,
                Activo = true,
                FechaCreacion = vOff.FechaLocal != default ? vOff.FechaLocal : DateTime.UtcNow
            };

            _context.Ventas.Add(venta);
            procesadas++;
        }

        await _context.SaveChangesAsync();
        return Ok(new { procesadas, mensaje = $"Se sincronizaron exitosamente {procesadas} venta(s) fuera de línea." });
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerHistorial([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _context.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Prenda)
            .AsQueryable();

        if (desde.HasValue)
        {
            var fechaInicioUtc = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);
            query = query.Where(v => v.FechaCreacion >= fechaInicioUtc);
        }

        if (hasta.HasValue)
        {
            var fechaFinUtc = DateTime.SpecifyKind(hasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(v => v.FechaCreacion <= fechaFinUtc);
        }

        var ventas = await query
            .OrderByDescending(v => v.FechaCreacion)
            .Select(v => new
            {
                v.Id,
                v.NumeroTicket,
                v.FechaCreacion,
                v.MetodoPago,
                v.Total,
                v.Activo,
                Cajero = v.Usuario != null ? v.Usuario.NombreCompleto : "Staff",
                TotalCosto = v.Detalles.Sum(d => d.Prenda != null ? d.Prenda.PrecioCosto * d.Cantidad : 0),
                GananciaNeta = v.Total - v.Detalles.Sum(d => d.Prenda != null ? d.Prenda.PrecioCosto * d.Cantidad : 0),
                TotalPiezas = v.Detalles.Sum(d => d.Cantidad),
                Lineas = v.Detalles.Select(d => new
                {
                    d.PrendaId,
                    Nombre = d.Prenda != null ? d.Prenda.Nombre : "Prenda",
                    Sku = d.Prenda != null ? d.Prenda.Sku : "",
                    Color = d.Prenda != null ? d.Prenda.Color : "",
                    Talla = d.Prenda != null ? (int)d.Prenda.Talla : 3,
                    d.Cantidad,
                    d.PrecioUnitario,
                    Subtotal = d.PrecioUnitario * d.Cantidad
                })
            })
            .ToListAsync();

        return Ok(ventas);
    }

    [HttpPost("{id}/cancelar")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CancelarVenta(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Prenda)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null) return NotFound(new { error = "Venta no encontrada." });
        if (!venta.Activo) return BadRequest(new { error = "Esta venta ya fue cancelada anteriormente." });

        foreach (var detalle in venta.Detalles)
        {
            if (detalle.Prenda != null)
            {
                detalle.Prenda.StockActual += detalle.Cantidad;
            }
        }

        venta.Activo = false;
        venta.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = $"Venta {venta.NumeroTicket} cancelada exitosamente." });
    }

    [HttpGet("exportar-excel")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportarVentasExcel([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _context.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Prenda)
            .AsQueryable();

        if (desde.HasValue)
        {
            var fechaInicioUtc = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);
            query = query.Where(v => v.FechaCreacion >= fechaInicioUtc);
        }

        if (hasta.HasValue)
        {
            var fechaFinUtc = DateTime.SpecifyKind(hasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(v => v.FechaCreacion <= fechaFinUtc);
        }

        var ventas = await query.OrderByDescending(v => v.FechaCreacion).ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("sep=,");
        builder.AppendLine("Folio,Fecha,Hora,Cajera,Metodo de Pago,Prendas,Total Costo,Total Venta,Ganancia Neta,Estado");

        foreach (var v in ventas)
        {
            var fecha = v.FechaCreacion.ToLocalTime().ToString("dd/MM/yyyy");
            var hora = v.FechaCreacion.ToLocalTime().ToString("HH:mm:ss");
            var metodo = v.MetodoPago.ToString();
            var totalCosto = v.Detalles.Sum(d => d.Prenda != null ? d.Prenda.PrecioCosto * d.Cantidad : 0);
            var ganancia = v.Total - totalCosto;
            var estado = v.Activo ? "Completada" : "Cancelada";
            var cajero = v.Usuario != null ? v.Usuario.NombreCompleto : "Staff";
            var prendasTexto = string.Join(" | ", v.Detalles.Select(d => $"{d.Cantidad}x {(d.Prenda != null ? d.Prenda.Nombre : "Item")} ({(d.Prenda != null ? d.Prenda.Talla.ToString() : "")})"));

            builder.AppendLine($"\"{v.NumeroTicket}\",\"{fecha}\",\"{hora}\",\"{cajero}\",\"{metodo}\",\"{prendasTexto}\",{totalCosto},{v.Total},{ganancia},\"{estado}\"");
        }

        var encoding = Encoding.UTF8;
        var preamble = encoding.GetPreamble();
        var bytes = encoding.GetBytes(builder.ToString());
        var finalBytes = preamble.Concat(bytes).ToArray();

        var nombreArchivo = $"MareaBrava_Ventas_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(finalBytes, "text/csv; charset=utf-8", nombreArchivo);
    }
}

public class CrearVentaDto
{
    public int UsuarioId { get; set; }
    public int MetodoPago { get; set; }
    public List<LineaVentaDto> Lineas { get; set; } = new();
}

public class LineaVentaDto
{
    public int PrendaId { get; set; }
    public int Cantidad { get; set; }
}

public class VentaOfflineDto
{
    public string FolioTemporal { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public int MetodoPago { get; set; }
    public decimal TotalEstimado { get; set; }
    public DateTime FechaLocal { get; set; }
    public List<LineaVentaDto> Lineas { get; set; } = new();
}