using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly MareaBravaDbContext _context;

    public UsuariosController(MareaBravaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var usuarios = await _context.Usuarios
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new
            {
                u.Id,
                u.NombreCompleto,
                u.Email,
                u.Rol,
                u.Activo,
                u.FechaCreacion
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpPost]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "El correo y la contraseña son obligatorios." });

        var existe = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (existe)
            return BadRequest(new { error = "Ya existe un usuario registrado con este correo electrónico." });

        var usuario = new Usuario
        {
            NombreCompleto = dto.NombreCompleto,
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Rol = (RolUsuario)dto.Rol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            usuario.Id,
            usuario.NombreCompleto,
            usuario.Email,
            usuario.Rol,
            usuario.Activo,
            mensaje = "Usuario creado exitosamente."
        });
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound(new { error = "Usuario no encontrado." });

        usuario.Activo = !usuario.Activo;
        usuario.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { usuario.Id, usuario.Activo, mensaje = usuario.Activo ? "Usuario reactivado." : "Usuario desactivado." });
    }

    [HttpPatch("{id}/password")]
    public async Task<IActionResult> CambiarPassword(int id, [FromBody] CambiarPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NuevoPassword))
            return BadRequest(new { error = "La nueva contraseña no puede estar vacía." });

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound(new { error = "Usuario no encontrado." });

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevoPassword);
        usuario.FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Contraseña actualizada con éxito." });
    }
}

public class CrearUsuarioDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Rol { get; set; }
}

public class CambiarPasswordDto
{
    public string NuevoPassword { get; set; } = string.Empty;
}