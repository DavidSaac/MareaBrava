namespace MareaBrava.WebUI.Utils;

// Resuelve dónde viven las fotos subidas: usa UPLOADS_PATH (disco persistente en producción)
// si está definida; si no, cae en wwwroot/uploads como antes (desarrollo local).
public static class UploadsPathResolver
{
    public static string Resolver(string webRootPath)
    {
        var configurado = Environment.GetEnvironmentVariable("UPLOADS_PATH");
        return string.IsNullOrWhiteSpace(configurado)
            ? Path.Combine(webRootPath, "uploads")
            : configurado;
    }
}
