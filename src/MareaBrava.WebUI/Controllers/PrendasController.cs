using Microsoft.AspNetCore.Mvc;
using MareaBrava.Application.DTOs;
using MareaBrava.Application.Interfaces;
using MareaBrava.Domain.Enums;

namespace MareaBrava.WebUI.Controllers;

public class SubirPrendaForm
{
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoPieza TipoPieza { get; set; }
    public TallaPrenda Talla { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; } = 3;
    public IFormFile? Imagen { get; set; }
}

public class AjustarStockDto
{
    public int Cantidad { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class PrendasController : ControllerBase
{
    private readonly IPrendaService _prendaService;
    private readonly IWebHostEnvironment _env;

    public PrendasController(IPrendaService prendaService, IWebHostEnvironment env)
    {
        _prendaService = prendaService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        var prendas = await _prendaService.ObtenerCatalogoActivoAsync();
        return Ok(prendas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var prenda = await _prendaService.ObtenerPorIdAsync(id);
        if (prenda == null) return NotFound(new { mensaje = "Prenda no encontrada." });
        return Ok(prenda);
    }

    [HttpGet("bajo-stock")]
    public async Task<IActionResult> ObtenerBajoStock()
    {
        var alertas = await _prendaService.ObtenerAlertasBajoStockAsync();
        return Ok(alertas);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Crear([FromForm] SubirPrendaForm form)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        string? rutaRelativaImagen = await GuardarImagenSiExiste(form.Imagen);

        var dto = new CrearPrendaDto
        {
            Sku = form.Sku.ToUpperInvariant(),
            Nombre = form.Nombre,
            Descripcion = form.Descripcion,
            TipoPieza = form.TipoPieza,
            Talla = form.Talla,
            Color = form.Color,
            PrecioCosto = form.PrecioCosto,
            PrecioVenta = form.PrecioVenta,
            StockActual = form.StockActual,
            StockMinimo = form.StockMinimo,
            ImagenUrl = rutaRelativaImagen
        };

        try
        {
            var resultado = await _prendaService.RegistrarNuevaPrendaAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Actualizar(int id, [FromForm] SubirPrendaForm form)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        string? rutaRelativaImagen = await GuardarImagenSiExiste(form.Imagen);

        var dto = new CrearPrendaDto
        {
            Sku = form.Sku.ToUpperInvariant(),
            Nombre = form.Nombre,
            Descripcion = form.Descripcion,
            TipoPieza = form.TipoPieza,
            Talla = form.Talla,
            Color = form.Color,
            PrecioCosto = form.PrecioCosto,
            PrecioVenta = form.PrecioVenta,
            StockActual = form.StockActual,
            StockMinimo = form.StockMinimo,
            ImagenUrl = rutaRelativaImagen
        };

        try
        {
            var resultado = await _prendaService.ActualizarPrendaAsync(id, dto);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:int}/stock")]
    public async Task<IActionResult> AjustarStock(int id, [FromBody] AjustarStockDto body)
    {
        try
        {
            var resultado = await _prendaService.AjustarStockAsync(id, body.Cantidad);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DarDeBaja(int id)
    {
        var exito = await _prendaService.DarDeBajaPrendaAsync(id);
        if (!exito) return NotFound(new { mensaje = "Prenda no encontrada para dar de baja." });
        return NoContent();
    }

    private async Task<string?> GuardarImagenSiExiste(IFormFile? imagen)
    {
        if (imagen == null || imagen.Length == 0) return null;

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(extension)) return null;

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var carpetaUploads = Path.Combine(webRoot, "uploads");

        if (!Directory.Exists(carpetaUploads))
        {
            Directory.CreateDirectory(carpetaUploads);
        }

        var nombreUnico = $"{Guid.NewGuid()}{extension}";
        var rutaFisica = Path.Combine(carpetaUploads, nombreUnico);

        using (var stream = new FileStream(rutaFisica, FileMode.Create))
        {
            await imagen.CopyToAsync(stream);
        }

        return $"/uploads/{nombreUnico}";
    }
}