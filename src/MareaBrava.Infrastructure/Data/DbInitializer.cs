using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;

namespace MareaBrava.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(MareaBravaDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 2. Sembrar Usuarios si la tabla está vacía
        if (!await context.Usuarios.AnyAsync())
        {
            await context.Usuarios.AddRangeAsync(
                new Usuario
                {
                    NombreCompleto = "Ximena",
                    Email = "ximena@mareabrava.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Rol = RolUsuario.Administrador,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                },
                new Usuario
                {
                    NombreCompleto = "Elizabeth",
                    Email = "elizabeth@mareabrava.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cajero123!"),
                    Rol = RolUsuario.Cajero,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
        }

        // 3. Sembrar Prendas iniciales si está vacío
        if (!await context.Prendas.AnyAsync())
        {
            await context.Prendas.AddRangeAsync(
                new Prenda
                {
                    Sku = "MB-800101",
                    Nombre = "Bikini Sunset Coral (Top + Bottom)",
                    Descripcion = "Traje de baño de 2 piezas elaborado con tela de secado rápido y protección UV.",
                    TipoPieza = TipoPieza.DosPiezas,
                    Talla = TallaPrenda.S,
                    Color = "Coral",
                    PrecioCosto = 180.00m,
                    PrecioVenta = 549.00m,
                    StockActual = 8,
                    StockMinimo = 3,
                    ImagenUrl = "https://images.unsplash.com/photo-1576995853123-5a10305d93c0?w=600",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                },
                new Prenda
                {
                    Sku = "MB-800102",
                    Nombre = "Top Deportivo Turquesa",
                    Descripcion = "Top individual con soporte reforzado ideal para deportes acuáticos.",
                    TipoPieza = TipoPieza.TopIndividual,
                    Talla = TallaPrenda.M,
                    Color = "Turquesa",
                    PrecioCosto = 130.00m,
                    PrecioVenta = 389.00m,
                    StockActual = 12,
                    StockMinimo = 3,
                    ImagenUrl = "https://images.unsplash.com/photo-1582639510494-c80b5de9f148?w=600",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                },
                new Prenda
                {
                    Sku = "MB-800103",
                    Nombre = "Traje Completo Deep Black",
                    Descripcion = "Traje entero con escote elegante y ajuste estilizador.",
                    TipoPieza = TipoPieza.UnaPieza,
                    Talla = TallaPrenda.M,
                    Color = "Negro",
                    PrecioCosto = 220.00m,
                    PrecioVenta = 699.00m,
                    StockActual = 6,
                    StockMinimo = 3,
                    ImagenUrl = "https://images.unsplash.com/photo-1563178406-4cdc2923acbc?w=600",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
        }
    }
}