namespace MareaBrava.Application.DTOs;

public class LoginRequestDto
{
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string TokenSimulado { get; set; } = string.Empty;
}