using Microsoft.EntityFrameworkCore;
using MareaBrava.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MareaBravaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapGet("/", () => "🌊 Sistema Marea Brava - API y Base de Datos activas.");

app.Run();