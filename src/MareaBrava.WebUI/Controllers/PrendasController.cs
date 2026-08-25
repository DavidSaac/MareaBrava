using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class PrendasController : ControllerBase
{
    private readonly MareaBravaDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PrendasController(MareaBravaDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas([FromQuery] int? tipo, [FromQuery] int? talla, [FromQuery] string? q)
    {
        var query = _context.Prendas.Where(p => p.Activo).AsQueryable();

        if (tipo.HasValue && tipo.Value > 0)
            query = query.Where(p => (int)p.TipoPieza == tipo.Value);

        if (talla.HasValue && talla.Value > 0)
            query = query.Where(p => (int)p.Talla == talla.Value);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.ToLower().Contains(q.ToLower()) || p.Sku.ToLower().Contains(q.ToLower()) || p.Color.ToLower().Contains(q.ToLower()));

        var prendas = await query
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new
            {
                p.Id,
                p.Sku,
                p.Nombre,
                p.TipoPieza,
                p.Talla,
                p.Color,
                p.StockActual,
                p.StockMinimo,
                p.PrecioCosto,
                p.PrecioVenta,
                p.ImagenUrl,
                p.Activo
            })
            .ToListAsync();

        return Ok(prendas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var prenda = await _context.Prendas.FindAsync(id);
        if (prenda == null || !prenda.Activo) return NotFound(new { error = "Prenda no encontrada." });
        return Ok(prenda);
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CrearPrenda([FromForm] CrearPrendaFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { error = "El SKU y Nombre son obligatorios." });

        var existeSku = await _context.Prendas.AnyAsync(p => p.Sku.ToLower() == dto.Sku.ToLower() && p.Activo);
        if (existeSku)
            return BadRequest(new { error = $"Ya existe una prenda activa con el SKU '{dto.Sku}'." });

        string? rutaImagen = null;
        if (dto.Imagen != null && dto.Imagen.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(dto.Imagen.FileName);
            var nombreArchivo = $"{dto.Sku}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
            var rutaFisica = Path.Combine(uploadsFolder, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await dto.Imagen.CopyToAsync(stream);
            }

            rutaImagen = $"/uploads/{nombreArchivo}";
        }

        var prenda = new Prenda
        {
            Sku = dto.Sku.Trim().ToUpper(),
            Nombre = dto.Nombre.Trim(),
            TipoPieza = (TipoPieza)dto.TipoPieza,
            Talla = (TallaPrenda)dto.Talla,
            Color = dto.Color.Trim(),
            StockActual = dto.StockActual,
            StockMinimo = dto.StockMinimo,
            PrecioCosto = dto.PrecioCosto,
            PrecioVenta = dto.PrecioVenta,
            ImagenUrl = rutaImagen,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Prendas.Add(prenda);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = prenda.Id }, prenda);
    }

    [HttpPut("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarPrenda(int id, [FromForm] EditarPrendaFormDto dto)
    {
        var prenda = await _context.Prendas.FindAsync(id);
        if (prenda == null || !prenda.Activo) return NotFound(new { error = "Prenda no encontrada." });

        prenda.Sku = dto.Sku.Trim().ToUpper();
        prenda.Nombre = dto.Nombre.Trim();
        prenda.TipoPieza = (TipoPieza)dto.TipoPieza;
        prenda.Talla = (TallaPrenda)dto.Talla;
        prenda.Color = dto.Color.Trim();
        prenda.StockActual = dto.StockActual;
        prenda.StockMinimo = dto.StockMinimo;
        prenda.PrecioCosto = dto.PrecioCosto;
        prenda.PrecioVenta = dto.PrecioVenta;
        prenda.FechaModificacion = DateTime.UtcNow;

        if (dto.Imagen != null && dto.Imagen.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(dto.Imagen.FileName);
            var nombreArchivo = $"{dto.Sku}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
            var rutaFisica = Path.Combine(uploadsFolder, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await dto.Imagen.CopyToAsync(stream);
            }

            prenda.ImagenUrl = $"/uploads/{nombreArchivo}";
        }

        await _context.SaveChangesAsync();
        return Ok(prenda);
    }

    [HttpPatch("{id}/stock")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AjustarStock(int id, [FromBody] AjustarStockDto dto)
    {
        var prenda = await _context.Prendas.FindAsync(id);
        if (prenda == null || !prenda.Activo) return NotFound(new { error = "Prenda no encontrada." });

        prenda.StockActual = Math.Max(0, prenda.StockActual + dto.Cantidad);
        prenda.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { prenda.Id, prenda.StockActual, mensaje = "Stock ajustado exitosamente." });
    }

    [HttpDelete("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EliminarPrenda(int id)
    {
        var prenda = await _context.Prendas.FindAsync(id);
        if (prenda == null || !prenda.Activo) return NotFound(new { error = "Prenda no encontrada." });

        prenda.Activo = false;
        prenda.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Prenda dada de baja exitosamente." });
    }

    [HttpGet("descargar-fotos-zip")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DescargarFotosZip([FromQuery] int? talla)
    {
        var query = _context.Prendas.Where(p => p.Activo && !string.IsNullOrEmpty(p.ImagenUrl)).AsQueryable();

        if (talla.HasValue && talla.Value > 0)
            query = query.Where(p => (int)p.Talla == talla.Value);

        var prendas = await query.ToListAsync();

        if (!prendas.Any())
            return NotFound(new { error = "No hay fotografías disponibles para los filtros seleccionados." });

        using (var memoryStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var p in prendas)
                {
                    var rutaRelativa = p.ImagenUrl!.TrimStart('/');
                    var rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);

                    if (System.IO.File.Exists(rutaFisica))
                    {
                        var nombreEnZip = $"{p.Sku}_{p.Nombre.Replace(" ", "_")}_{p.Color}{Path.GetExtension(rutaFisica)}";
                        archive.CreateEntryFromFile(rutaFisica, nombreEnZip);
                    }
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            var nombreArchivo = $"MareaBrava_Fotos_{(talla.HasValue && talla.Value > 0 ? $"Talla_{talla.Value}" : "CatalogoCompleto")}_{DateTime.Now:yyyyMMdd}.zip";
            return File(memoryStream.ToArray(), "application/zip", nombreArchivo);
        }
    }

    [HttpGet("exportar-excel")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportarInventarioExcel()
    {
        var prendas = await _context.Prendas
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("sep=,");
        builder.AppendLine("SKU,Modelo,Tipo,Talla,Color,Stock Actual,Stock Minimo,Precio Costo,Precio Venta,Valor Total Costo,Valor Total Venta");

        foreach (var p in prendas)
        {
            var valorCosto = p.PrecioCosto * p.StockActual;
            var valorVenta = p.PrecioVenta * p.StockActual;
            builder.AppendLine($"\"{p.Sku}\",\"{p.Nombre}\",\"{p.TipoPieza}\",\"{p.Talla}\",\"{p.Color}\",{p.StockActual},{p.StockMinimo},{p.PrecioCosto},{p.PrecioVenta},{valorCosto},{valorVenta}");
        }

        var encoding = Encoding.UTF8;
        var preamble = encoding.GetPreamble();
        var bytes = encoding.GetBytes(builder.ToString());
        var finalBytes = preamble.Concat(bytes).ToArray();

        var nombreArchivo = $"MareaBrava_Inventario_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(finalBytes, "text/csv; charset=utf-8", nombreArchivo);
    }
}

public class CrearPrendaFormDto
{
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int TipoPieza { get; set; }
    public int Talla { get; set; }
    public string Color { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public IFormFile? Imagen { get; set; }
}

public class EditarPrendaFormDto : CrearPrendaFormDto { }

public class AjustarStockDto
{
    public int Cantidad { get; set; }
}