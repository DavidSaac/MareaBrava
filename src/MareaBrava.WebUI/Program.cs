using Microsoft.EntityFrameworkCore;
using MareaBrava.Application.Interfaces;
using MareaBrava.Application.Services;
using MareaBrava.Infrastructure.Data;
using MareaBrava.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Conexión a la base de datos SQLite
builder.Services.AddDbContext<MareaBravaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Inyección de dependencias (Clean Architecture)
builder.Services.AddScoped<IPrendaRepository, PrendaRepository>();
builder.Services.AddScoped<IPrendaService, PrendaService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllers();

app.MapGet("/", () => "🌊 Sistema Marea Brava - API y Base de Datos activas.");

app.Run();