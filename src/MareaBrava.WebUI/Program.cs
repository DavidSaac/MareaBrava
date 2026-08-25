using MareaBrava.Infrastructure;
using MareaBrava.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar CORS para permitir Cloudflare Pages y pruebas locales
builder.Services.AddCors(options =>
{
    options.AddPolicy("CloudflarePolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "https://localhost:5001",
                "https://mareabrava.pages.dev",
                "https://mareabrava.mx",
                "https://www.mareabrava.mx",
                "https://mareabrava.com",
                "https://www.mareabrava.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MareaBrava.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
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
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// 7. Mapear Controladores
app.MapControllers();

// 8. Fallback para servir index.html si se consulta la raíz directa
app.MapFallbackToFile("index.html");

app.Run();