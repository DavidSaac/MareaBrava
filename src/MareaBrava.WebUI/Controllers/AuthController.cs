using System.Security.Claims;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MareaBrava.Application.DTOs;
using MareaBrava.Infrastructure.Data;

namespace MareaBrava.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, LoginAttempt> LoginAttempts = new();
    private static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(10);
    private const int MaxFailedAttempts = 5;
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

        var attemptKey = $"{HttpContext.Connection.RemoteIpAddress}|{request.Correo.Trim().ToLowerInvariant()}";
        if (LoginAttempts.TryGetValue(attemptKey, out var previous) && previous.IsBlocked(LoginWindow, MaxFailedAttempts))
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Demasiados intentos. Espera unos minutos e inténtalo nuevamente." });

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Correo.ToLower() && u.Activo);

        if (usuario == null)
        {
            RegistrarIntentoFallido(attemptKey);
            return Unauthorized(new { error = "Credenciales incorrectas o usuario inactivo." });
        }

        var passwordValida = false;
        if (usuario.PasswordHash.StartsWith("$2", StringComparison.Ordinal))
        {
            passwordValida = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
        }
        else if (usuario.PasswordHash == request.Password)
        {
            // Upgrade legacy seed credentials after the first successful login.
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            usuario.FechaModificacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            passwordValida = true;
        }

        if (!passwordValida)
        {
            RegistrarIntentoFallido(attemptKey);
            return Unauthorized(new { error = "Credenciales incorrectas o usuario inactivo." });
        }

        LoginAttempts.TryRemove(attemptKey, out _);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });

        var respuesta = new LoginResponseDto
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Correo = usuario.Email,
            Rol = usuario.Rol.ToString(),
            TokenSimulado = string.Empty
        };

        return Ok(respuesta);
    }

    private static void RegistrarIntentoFallido(string key)
    {
        LoginAttempts.AddOrUpdate(key,
            _ => new LoginAttempt(1, DateTime.UtcNow),
            (_, current) => current.IsExpired(LoginWindow)
                ? new LoginAttempt(1, DateTime.UtcNow)
                : current with { Count = current.Count + 1 });
    }

    private sealed record LoginAttempt(int Count, DateTime FirstAttemptUtc)
    {
        public bool IsExpired(TimeSpan window) => DateTime.UtcNow - FirstAttemptUtc > window;
        public bool IsBlocked(TimeSpan window, int maxAttempts) => !IsExpired(window) && Count >= maxAttempts;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}