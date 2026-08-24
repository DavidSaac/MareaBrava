using MareaBrava.Domain.Entities;
using MareaBrava.Domain.Enums;

namespace MareaBrava.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(MareaBravaDbContext context)
    {
        // 1. Crear usuario Administrador si no existe
        if (!context.Usuarios.Any())
        {
            context.Usuarios.Add(new Usuario
            {
                NombreCompleto = "Administrador Marea Brava",
                Email = "admin@mareabrava.com",
                PasswordHash = "admin123", // Para desarrollo inicial
                Rol = RolUsuario.Administrador,
                Activo = true
            });
            await context.SaveChangesAsync();
        }

        // 2. Crear prendas iniciales de ejemplo si no existen
        if (!context.Prendas.Any())
        {
            var prendasIniciales = new List<Prenda>
            {
                new Prenda
                {
                    Sku = "BIK-COR-001-S",
                    Nombre = "Bikini Sunset Coral (Top + Bottom)",
                    Descripcion = "Conjunto de dos piezas en tono coral vibrante con tirantes ajustables.",
                    TipoPieza = TipoPieza.DosPiezas,
                    Talla = TallaPrenda.S,
                    Color = "Coral",
                    PrecioCosto = 280.00m,
                    PrecioVenta = 590.00m,
                    StockActual = 12,
                    StockMinimo = 3,
                    Activo = true
                },
                new Prenda
                {
                    Sku = "TRA-NEG-002-M",
                    Nombre = "Traje Completo Deep Black",
                    Descripcion = "Traje de baño completo con escote en espalda y control de abdomen.",
                    TipoPieza = TipoPieza.UnaPieza,
                    Talla = TallaPrenda.M,
                    Color = "Negro",
                    PrecioCosto = 320.00m,
                    PrecioVenta = 680.00m,
                    StockActual = 8,
                    StockMinimo = 2,
                    Activo = true
                },
                new Prenda
                {
                    Sku = "TOP-TUR-003-L",
                    Nombre = "Top Deportivo Turquesa",
                    Descripcion = "Top individual tipo crop con protección UV para deportes acuáticos.",
                    TipoPieza = TipoPieza.TopIndividual,
                    Talla = TallaPrenda.L,
                    Color = "Turquesa",
                    PrecioCosto = 190.00m,
                    PrecioVenta = 420.00m,
                    StockActual = 2, // Alerta: bajo stock
                    StockMinimo = 3,
                    Activo = true
                }
            };

            context.Prendas.AddRange(prendasIniciales);
            await context.SaveChangesAsync();
        }
    }
}