using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InformacionTiendaController : ControllerBase
{
    private readonly MareaBravaDbContext _context;

    public InformacionTiendaController(MareaBravaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Obtener()
    {
        var informacion = await _context.InformacionTienda
            .Where(i => i.Id == 1 && i.Activo)
            .Select(i => new { i.TextoPromocional })
            .SingleOrDefaultAsync();

        return informacion == null
            ? NotFound(new { error = "No se encontró la información de la tienda." })
            : Ok(informacion);
    }

    [HttpPut]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Actualizar([FromBody] InformacionTiendaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TextoPromocional) || request.TextoPromocional.Length > 2000)
            return BadRequest(new { error = "La información debe tener entre 1 y 2000 caracteres." });

        var informacion = await _context.InformacionTienda.SingleOrDefaultAsync(i => i.Id == 1);
        if (informacion == null)
        {
            informacion = new InformacionTienda { Id = 1, TextoPromocional = request.TextoPromocional, Activo = true };
            _context.InformacionTienda.Add(informacion);
        }
        else
        {
            informacion.TextoPromocional = request.TextoPromocional;
            informacion.Activo = true;
            informacion.FechaModificacion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { informacion.TextoPromocional });
    }
}

public record InformacionTiendaRequest(string TextoPromocional);