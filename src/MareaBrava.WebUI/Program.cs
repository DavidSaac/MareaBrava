using MareaBrava.Infrastructure;
using MareaBrava.Infrastructure.Data;
using MareaBrava.WebUI.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");

// 1. Configurar CORS para permitir Cloudflare Pages y pruebas locales
builder.Services.AddCors(options =>
{
    options.AddPolicy("CloudflarePolicy", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();
        policy.WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MareaBrava.Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = builder.Environment.IsDevelopment()
            ? SameSiteMode.Lax
            : SameSiteMode.None;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// 2. Inyectar Capa de Infraestructura (EF Core, SQLite, Repositorios)
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Controladores API con serialización JSON estándar
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// 4. Inicializar base de datos y datos semilla automáticamente
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MareaBravaDbContext>();
    await DbInitializer.SeedAsync(context);
}

// 5. Aplicar CORS
app.UseCors("CloudflarePolicy");

// 6. Servir archivos estáticos (uploads, fotos, etc.)
// Fotos nuevas: si UPLOADS_PATH apunta a un disco persistente (ej. Render), se sirven desde ahí;
// las fotos ya existentes en wwwroot/uploads (horneadas en la imagen) siguen funcionando por el fallback de abajo.
var carpetaUploads = UploadsPathResolver.Resolver(app.Environment.WebRootPath);
Directory.CreateDirectory(carpetaUploads);
var carpetaUploadsWwwroot = Path.Combine(app.Environment.WebRootPath, "uploads");
if (!string.Equals(Path.GetFullPath(carpetaUploads), Path.GetFullPath(carpetaUploadsWwwroot), StringComparison.OrdinalIgnoreCase))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(carpetaUploads),
        RequestPath = "/uploads"
    });
}
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// 7. Mapear Controladores
app.MapControllers();

// 8. Fallback para servir index.html si se consulta la raíz directa
app.MapFallbackToFile("index.html");

app.Run();