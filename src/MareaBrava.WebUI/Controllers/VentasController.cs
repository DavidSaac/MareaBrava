using Microsoft.AspNetCore.Mvc;
using MareaBrava.Application.DTOs;
using MareaBrava.Application.Interfaces;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;

    public VentasController(IVentaService ventaService)
    {
        _ventaService = ventaService;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerHistorial()
    {
        var ventas = await _ventaService.ObtenerHistorialVentasAsync();
        return Ok(ventas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var venta = await _ventaService.ObtenerPorIdAsync(id);
        if (venta == null) return NotFound(new { mensaje = $"Venta con ID {id} no encontrada." });
        return Ok(venta);
    }

    [HttpGet("ticket/{numeroTicket}")]
    public async Task<IActionResult> ObtenerPorTicket(string numeroTicket)
    {
        var venta = await _ventaService.ObtenerPorTicketAsync(numeroTicket);
        if (venta == null) return NotFound(new { mensaje = $"Ticket '{numeroTicket}' no encontrado." });
        return Ok(venta);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarVentaDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var ventaRealizada = await _ventaService.ProcesarVentaAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = ventaRealizada.Id }, ventaRealizada);
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
}