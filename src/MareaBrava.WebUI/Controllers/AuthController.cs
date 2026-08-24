using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Application.DTOs;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MareaBravaDbContext _context;

    public AuthController(MareaBravaDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "El correo y la contraseña son obligatorios." });
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Correo.ToLower() && u.Activo);

        if (usuario == null || usuario.PasswordHash != request.Password)
        {
            return Unauthorized(new { error = "Credenciales incorrectas o usuario inactivo." });
        }

        var respuesta = new LoginResponseDto
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Correo = usuario.Email,
            Rol = usuario.Rol.ToString(),
            TokenSimulado = $"token_{Guid.NewGuid()}"
        };

        return Ok(respuesta);
    }
}