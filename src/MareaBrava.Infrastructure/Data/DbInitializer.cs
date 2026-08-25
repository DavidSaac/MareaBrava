using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;

namespace MareaBrava.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(MareaBravaDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await EnsureCategoriesSchemaAsync(context);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS Gastos (Id INTEGER NOT NULL CONSTRAINT PK_Gastos PRIMARY KEY AUTOINCREMENT, Concepto TEXT NOT NULL, Monto TEXT NOT NULL, FechaGasto TEXT NOT NULL, Observaciones TEXT NULL, FechaCreacion TEXT NOT NULL, FechaModificacion TEXT NULL, Activo INTEGER NOT NULL);");

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
                    CategoriaId = await GetCategoryIdAsync(context, "Bikinis (2 Piezas)"),
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
                    CategoriaId = await GetCategoryIdAsync(context, "Tops Individuales"),
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
                    CategoriaId = await GetCategoryIdAsync(context, "Trajes Completos (1 Pieza)"),
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

    private static async Task EnsureCategoriesSchemaAsync(MareaBravaDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CategoriasProductos (Id INTEGER NOT NULL CONSTRAINT PK_CategoriasProductos PRIMARY KEY AUTOINCREMENT, Nombre TEXT NOT NULL, FechaCreacion TEXT NOT NULL, FechaModificacion TEXT NULL, Activo INTEGER NOT NULL);");
        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_CategoriasProductos_Nombre ON CategoriasProductos (Nombre);");

        var columns = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Prendas') WHERE name = 'CategoriaId'").ToListAsync();
        if (columns.Count == 0)
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Prendas ADD COLUMN CategoriaId INTEGER NULL;");

        var categorias = new[]
        {
            "Trajes Completos (1 Pieza)",
            "Bikinis (2 Piezas)",
            "Tops Individuales",
            "Bottoms Individuales",
            "Pareos y Accesorios"
        };

        foreach (var categoria in categorias)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT OR IGNORE INTO CategoriasProductos (Nombre, FechaCreacion, Activo) VALUES ({categoria}, {DateTime.UtcNow}, 1)");
        }

        await context.Database.ExecuteSqlRawAsync("UPDATE Prendas SET CategoriaId = (SELECT Id FROM CategoriasProductos WHERE Nombre = 'Trajes Completos (1 Pieza)') WHERE CategoriaId IS NULL AND TipoPieza = 1;");
        await context.Database.ExecuteSqlRawAsync("UPDATE Prendas SET CategoriaId = (SELECT Id FROM CategoriasProductos WHERE Nombre = 'Bikinis (2 Piezas)') WHERE CategoriaId IS NULL AND TipoPieza = 2;");
        await context.Database.ExecuteSqlRawAsync("UPDATE Prendas SET CategoriaId = (SELECT Id FROM CategoriasProductos WHERE Nombre = 'Tops Individuales') WHERE CategoriaId IS NULL AND TipoPieza = 3;");
        await context.Database.ExecuteSqlRawAsync("UPDATE Prendas SET CategoriaId = (SELECT Id FROM CategoriasProductos WHERE Nombre = 'Bottoms Individuales') WHERE CategoriaId IS NULL AND TipoPieza = 4;");
        await context.Database.ExecuteSqlRawAsync("UPDATE Prendas SET CategoriaId = (SELECT Id FROM CategoriasProductos WHERE Nombre = 'Pareos y Accesorios') WHERE CategoriaId IS NULL AND TipoPieza = 5;");
    }

    private static async Task<int> GetCategoryIdAsync(MareaBravaDbContext context, string name)
    {
        return await context.CategoriasProductos
            .Where(c => c.Nombre == name)
            .Select(c => c.Id)
            .SingleAsync();
    }
}