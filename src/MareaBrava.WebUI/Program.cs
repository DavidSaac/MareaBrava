using Microsoft.EntityFrameworkCore;
using MareaBrava.Application.Interfaces;
using MareaBrava.Application.Services;
using MareaBrava.Infrastructure.Data;
using MareaBrava.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Base de datos SQLite
builder.Services.AddDbContext<MareaBravaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositorios y Servicios
builder.Services.AddScoped<IPrendaRepository, PrendaRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IPrendaService, PrendaService>();
builder.Services.AddScoped<IVentaService, VentaService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Sembrar datos iniciales
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MareaBravaDbContext>();
    await DbInitializer.SeedAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Marea Brava API v1");
        c.RoutePrefix = "swagger"; // Swagger estará disponible en /swagger
    });
}

app.UseDefaultFiles(); // Busca automáticamente index.html en wwwroot
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

app.Run();