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
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private readonly MareaBravaDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PrendasController(MareaBravaDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
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
                p.CategoriaId,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null,
                p.Talla,
                p.Color,
                p.StockActual,
                p.StockMinimo,
                p.PrecioCosto,
                p.PrecioVenta,
                p.ImagenUrl,
                p.FechaCreacion,
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

        if (dto.StockActual < 0 || dto.StockMinimo < 0 || dto.PrecioCosto < 0 || dto.PrecioVenta <= 0)
            return BadRequest(new { error = "Stock y precios deben tener valores válidos." });

        if (!dto.CategoriaId.HasValue)
            return BadRequest(new { error = "Selecciona una categoría activa." });

        var categoriaSeleccionada = await _context.CategoriasProductos.FirstOrDefaultAsync(c => c.Id == dto.CategoriaId && c.Activo);
        if (categoriaSeleccionada == null)
            return BadRequest(new { error = "Selecciona una categoría activa." });

        var existeSku = await _context.Prendas.AnyAsync(p => p.Sku.ToLower() == dto.Sku.ToLower() && p.Activo);
        if (existeSku)
            return BadRequest(new { error = $"Ya existe una prenda activa con el SKU '{dto.Sku}'." });

        string? rutaImagen = null;
        if (dto.Imagen != null && dto.Imagen.Length > 0)
        {
            if (!TryGetImageExtension(dto.Imagen, out var extension))
                return BadRequest(new { error = "La imagen debe ser JPG, PNG o WEBP y no superar 5 MB." });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
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
            TipoPieza = categoriaSeleccionada.TipoPieza,
            CategoriaId = dto.CategoriaId,
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

        if (string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { error = "El SKU y Nombre son obligatorios." });

        if (dto.StockActual < 0 || dto.StockMinimo < 0 || dto.PrecioCosto < 0 || dto.PrecioVenta <= 0)
            return BadRequest(new { error = "Stock y precios deben tener valores válidos." });

        if (!dto.CategoriaId.HasValue)
            return BadRequest(new { error = "Selecciona una categoría activa." });

        var categoriaSeleccionada = await _context.CategoriasProductos.FirstOrDefaultAsync(c => c.Id == dto.CategoriaId && c.Activo);
        if (categoriaSeleccionada == null)
            return BadRequest(new { error = "Selecciona una categoría activa." });

        var existeSku = await _context.Prendas.AnyAsync(p => p.Id != id && p.Activo && p.Sku.ToLower() == dto.Sku.ToLower());
        if (existeSku)
            return BadRequest(new { error = $"Ya existe una prenda activa con el SKU '{dto.Sku}'." });

        prenda.Sku = dto.Sku.Trim().ToUpper();
        prenda.Nombre = dto.Nombre.Trim();
        prenda.TipoPieza = categoriaSeleccionada.TipoPieza;
        prenda.CategoriaId = dto.CategoriaId;
        prenda.Talla = (TallaPrenda)dto.Talla;
        prenda.Color = dto.Color.Trim();
        prenda.StockActual = dto.StockActual;
        prenda.StockMinimo = dto.StockMinimo;
        prenda.PrecioCosto = dto.PrecioCosto;
        prenda.PrecioVenta = dto.PrecioVenta;
        prenda.FechaModificacion = DateTime.UtcNow;

        if (dto.Imagen != null && dto.Imagen.Length > 0)
        {
            if (!TryGetImageExtension(dto.Imagen, out var extension))
                return BadRequest(new { error = "La imagen debe ser JPG, PNG o WEBP y no superar 5 MB." });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
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

    private static bool TryGetImageExtension(IFormFile image, out string extension)
    {
        extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };

        return image.Length <= MaxImageBytes
            && allowedExtensions.Contains(extension)
            && allowedContentTypes.Contains(image.ContentType, StringComparer.OrdinalIgnoreCase);
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
    public async Task<IActionResult> DescargarFotosZip(
        [FromQuery] int? talla,
        [FromQuery] int? categoriaId,
        [FromQuery] string? q,
        [FromQuery] string? stock)
    {
        var query = _context.Prendas.Where(p => p.Activo && !string.IsNullOrEmpty(p.ImagenUrl)).AsQueryable();

        if (talla.HasValue && talla.Value > 0)
            query = query.Where(p => (int)p.Talla == talla.Value);
        if (categoriaId.HasValue && categoriaId.Value > 0)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.ToLower().Contains(q.ToLower()) || p.Sku.ToLower().Contains(q.ToLower()) || p.Color.ToLower().Contains(q.ToLower()));
        if (stock == "bajo")
            query = query.Where(p => p.StockActual <= 3 && p.StockActual > 0);
        if (stock == "agotado")
            query = query.Where(p => p.StockActual == 0);

        var prendas = await query.ToListAsync();

        if (!prendas.Any())
            return NotFound(new { error = "No hay productos activos con fotografía asignada para los filtros seleccionados." });

        // Clasifica cada prenda para poder informar con precisión qué se incluyó y qué no.
        var encontradas = new List<(Prenda Prenda, string RutaFisica)>();
        var externas = new List<string>();
        var noEncontradas = new List<string>();

        foreach (var p in prendas)
        {
            var imagenUrl = p.ImagenUrl!;

            if (imagenUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imagenUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                externas.Add(p.Sku);
                continue;
            }

            var rutaRelativa = imagenUrl.TrimStart('/');
            var rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);

            if (System.IO.File.Exists(rutaFisica))
                encontradas.Add((p, rutaFisica));
            else
                noEncontradas.Add(p.Sku);
        }

        if (!encontradas.Any())
        {
            return NotFound(new
            {
                error = "No se encontró ninguna fotografía descargable para los productos seleccionados.",
                fotosExternas = externas,
                fotosNoEncontradasFisicamente = noEncontradas
            });
        }

        using (var memoryStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var nombresUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var (p, rutaFisica) in encontradas)
                {
                    var nombreBase = SanearNombreArchivo($"{p.Sku}_{p.Nombre}_{p.Color}");
                    var nombreEnZip = $"{nombreBase}{Path.GetExtension(rutaFisica)}";

                    // Evita colisiones de nombre dentro del ZIP (p. ej. mismo SKU/nombre/color).
                    var sufijo = 2;
                    while (!nombresUsados.Add(nombreEnZip))
                        nombreEnZip = $"{nombreBase}_{sufijo++}{Path.GetExtension(rutaFisica)}";

                    archive.CreateEntryFromFile(rutaFisica, nombreEnZip);
                }

                if (externas.Any() || noEncontradas.Any())
                {
                    var reporte = new StringBuilder();
                    reporte.AppendLine("Fotografías NO incluidas en este ZIP:");
                    if (externas.Any())
                    {
                        reporte.AppendLine();
                        reporte.AppendLine("- URLs externas (datos de prueba, no son archivos propios del sistema):");
                        foreach (var sku in externas) reporte.AppendLine($"  * {sku}");
                    }
                    if (noEncontradas.Any())
                    {
                        reporte.AppendLine();
                        reporte.AppendLine("- Registradas en el sistema pero el archivo físico ya no existe en el servidor:");
                        foreach (var sku in noEncontradas) reporte.AppendLine($"  * {sku}");
                    }

                    var entradaReporte = archive.CreateEntry("FOTOS_NO_INCLUIDAS.txt");
                    using var writer = new StreamWriter(entradaReporte.Open(), Encoding.UTF8);
                    writer.Write(reporte.ToString());
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            var nombreArchivo = $"MareaBrava_Fotos_{(talla.HasValue && talla.Value > 0 ? $"Talla_{talla.Value}" : "CatalogoCompleto")}_{DateTime.Now:yyyyMMdd}.zip";

            Response.Headers["X-Fotos-Incluidas"] = encontradas.Count.ToString();
            Response.Headers["X-Fotos-Omitidas"] = (externas.Count + noEncontradas.Count).ToString();

            return File(memoryStream.ToArray(), "application/zip", nombreArchivo);
        }
    }

    private static string SanearNombreArchivo(string nombre)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = new string(nombre.Select(c => invalidos.Contains(c) ? '_' : c).ToArray());
        return limpio.Replace(" ", "_");
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
    // TipoPieza ya no se recibe del formulario: se deriva de la Categoria seleccionada en el backend.
    public int? CategoriaId { get; set; }
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