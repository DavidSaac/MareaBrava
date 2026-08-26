using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly MareaBravaDbContext _context;

    public CategoriasController(MareaBravaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerActivas()
    {
        var categorias = await _context.CategoriasProductos
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new { c.Id, c.Nombre })
            .ToListAsync();

        return Ok(categorias);
    }

    [HttpGet("todas")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ObtenerTodas()
    {
        var categorias = await _context.CategoriasProductos
            .OrderBy(c => c.Nombre)
            .Select(c => new { c.Id, c.Nombre, c.Activo, c.TipoPieza, prendasActivas = c.Prendas.Count(p => p.Activo) })
            .ToListAsync();

        return Ok(categorias);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear([FromBody] CategoriaRequest request)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 100)
            return BadRequest(new { error = "El nombre debe tener entre 1 y 100 caracteres." });

        if (!Enum.IsDefined(typeof(TipoPieza), request.TipoPieza))
            return BadRequest(new { error = "Selecciona un tipo de pieza válido para la categoría." });

        if (await _context.CategoriasProductos.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower()))
            return Conflict(new { error = "Ya existe una categoría con ese nombre." });

        var categoria = new CategoriaProducto { Nombre = nombre, Activo = true, TipoPieza = (TipoPieza)request.TipoPieza };
        _context.CategoriasProductos.Add(categoria);
        await _context.SaveChangesAsync();
        return Ok(new { categoria.Id, categoria.Nombre, categoria.Activo, categoria.TipoPieza });
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var categoria = await _context.CategoriasProductos.FindAsync(id);
        if (categoria == null) return NotFound(new { error = "Categoría no encontrada." });

        if (categoria.Activo && await _context.Prendas.AnyAsync(p => p.Activo && p.CategoriaId == id))
            return BadRequest(new { error = "No se puede dar de baja una categoría con prendas activas. Reasigna primero sus productos." });

        categoria.Activo = !categoria.Activo;
        categoria.FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { categoria.Id, categoria.Activo });
    }
}

public record CategoriaRequest(string? Nombre, int TipoPieza);
