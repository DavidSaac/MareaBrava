using Microsoft.AspNetCore.Mvc;
using MareaBrava.Application.DTOs;
using MareaBrava.Application.Interfaces;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrendasController : ControllerBase
{
    private readonly IPrendaService _prendaService;

    public PrendasController(IPrendaService prendaService)
    {
        _prendaService = prendaService;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        var prendas = await _prendaService.ObtenerCatalogoAsync();
        return Ok(prendas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var prenda = await _prendaService.ObtenerPorIdAsync(id);
        if (prenda == null) return NotFound(new { mensaje = $"Prenda con ID {id} no encontrada." });
        return Ok(prenda);
    }

    [HttpGet("bajo-stock")]
    public async Task<IActionResult> ObtenerBajoStock()
    {
        var prendas = await _prendaService.ObtenerPrendasBajoStockAsync();
        return Ok(prendas);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearPrendaDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var prendaCreada = await _prendaService.CrearPrendaAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = prendaCreada.Id }, prendaCreada);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CrearPrendaDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _prendaService.ActualizarPrendaAsync(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _prendaService.EliminarPrendaAsync(id);
        return NoContent();
    }
}