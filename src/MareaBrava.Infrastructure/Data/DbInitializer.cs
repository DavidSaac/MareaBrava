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
        await EnsureStoreInformationSchemaAsync(context);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS Gastos (Id INTEGER NOT NULL CONSTRAINT PK_Gastos PRIMARY KEY AUTOINCREMENT, Concepto TEXT NOT NULL, Monto TEXT NOT NULL, FechaGasto TEXT NOT NULL, Observaciones TEXT NULL, FechaCreacion TEXT NOT NULL, FechaModificacion TEXT NULL, Activo INTEGER NOT NULL);");
        await EnsureSalesAndReservationsSchemaAsync(context);
        await EnsureNullableSaleProductAsync(context);

        // Siembra de usuarios Demo deshabilitada: producción crea cuentas reales vía UsuariosController.
        // Los usuarios Demo existentes (si los hay) no se tocan aquí; se desactivan manualmente cuando corresponda.
        await SeedInitialAdminAsync(context);

        // Siembra de prendas Demo (MB-800101/102/103) deshabilitada: una BD nueva no debe crear productos de prueba.
    }

    private static async Task EnsureCategoriesSchemaAsync(MareaBravaDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CategoriasProductos (Id INTEGER NOT NULL CONSTRAINT PK_CategoriasProductos PRIMARY KEY AUTOINCREMENT, Nombre TEXT NOT NULL, FechaCreacion TEXT NOT NULL, FechaModificacion TEXT NULL, Activo INTEGER NOT NULL);");
        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_CategoriasProductos_Nombre ON CategoriasProductos (Nombre);");

        var columns = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Prendas') WHERE name = 'CategoriaId'").ToListAsync();
        if (columns.Count == 0)
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Prendas ADD COLUMN CategoriaId INTEGER NULL;");

        // Columna TipoPieza en CategoriasProductos: nuevo, valor por defecto Accesorio (5) para categorías ya existentes sin mapeo conocido.
        var categoriaTipoPiezaColumns = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('CategoriasProductos') WHERE name = 'TipoPieza'").ToListAsync();
        if (categoriaTipoPiezaColumns.Count == 0)
            await context.Database.ExecuteSqlRawAsync($"ALTER TABLE CategoriasProductos ADD COLUMN TipoPieza INTEGER NOT NULL DEFAULT {(int)TipoPieza.Accesorio};");

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

        // Correspondencia fija TipoPieza <-> categorías semilla (no toca nombres/IDs ni otras categorías creadas por el Admin).
        await context.Database.ExecuteSqlRawAsync($"UPDATE CategoriasProductos SET TipoPieza = {(int)TipoPieza.UnaPieza} WHERE Nombre = 'Trajes Completos (1 Pieza)';");
        await context.Database.ExecuteSqlRawAsync($"UPDATE CategoriasProductos SET TipoPieza = {(int)TipoPieza.DosPiezas} WHERE Nombre = 'Bikinis (2 Piezas)';");
        await context.Database.ExecuteSqlRawAsync($"UPDATE CategoriasProductos SET TipoPieza = {(int)TipoPieza.TopIndividual} WHERE Nombre = 'Tops Individuales';");
        await context.Database.ExecuteSqlRawAsync($"UPDATE CategoriasProductos SET TipoPieza = {(int)TipoPieza.BottomIndividual} WHERE Nombre = 'Bottoms Individuales';");
        await context.Database.ExecuteSqlRawAsync($"UPDATE CategoriasProductos SET TipoPieza = {(int)TipoPieza.Accesorio} WHERE Nombre = 'Pareos y Accesorios';");
    }

    private static async Task<int> GetCategoryIdAsync(MareaBravaDbContext context, string name)
    {
        return await context.CategoriasProductos
            .Where(c => c.Nombre == name)
            .Select(c => c.Id)
            .SingleAsync();
    }

    private static async Task EnsureStoreInformationSchemaAsync(MareaBravaDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS InformacionTienda (Id INTEGER NOT NULL CONSTRAINT PK_InformacionTienda PRIMARY KEY AUTOINCREMENT, TextoPromocional TEXT NOT NULL, FechaCreacion TEXT NOT NULL, FechaModificacion TEXT NULL, Activo INTEGER NOT NULL);");

        var informacionInicial = "🔥 Bikinis desde $199 🔥\n+300 modelos disponibles\n\n👙 Bikinis · Pareos · Vestidos de playa\n🕶️ Lentes · Mascadas · Bolsas\n\n📲 Catálogo completo por WhatsApp\n✨ Nuevos modelos cada semana";
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO InformacionTienda (Id, TextoPromocional, FechaCreacion, Activo) SELECT 1, {informacionInicial}, {DateTime.UtcNow}, 1 WHERE NOT EXISTS (SELECT 1 FROM InformacionTienda WHERE Id = 1);");
    }

    // Crea el primer Administrador real solo si Usuarios está vacía; credenciales exclusivamente por variables de entorno.
    private static async Task SeedInitialAdminAsync(MareaBravaDbContext context)
    {
        if (await context.Usuarios.AnyAsync())
            return;

        var email = Environment.GetEnvironmentVariable("INITIAL_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("INITIAL_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        await context.Usuarios.AddAsync(new Usuario
        {
            NombreCompleto = "Administrador",
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = RolUsuario.Administrador,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static async Task EnsureSalesAndReservationsSchemaAsync(MareaBravaDbContext context)
    {
        var ventaColumns = new (string Name, string Definition)[]
        {
            ("Subtotal", "TEXT NOT NULL DEFAULT 0"),
            ("DescuentoPorcentaje", "TEXT NOT NULL DEFAULT 0"),
            ("DescuentoMonto", "TEXT NOT NULL DEFAULT 0"),
            ("EfectivoRecibido", "TEXT NOT NULL DEFAULT 0"),
            ("Cambio", "TEXT NOT NULL DEFAULT 0"),
            ("EsVentaDesarmada", "INTEGER NOT NULL DEFAULT 0"),
            ("NotaAdministrativa", "TEXT NULL")
        };
        foreach (var column in ventaColumns)
            await AddColumnIfMissingAsync(context, "Ventas", column.Name, column.Definition);

        await AddColumnIfMissingAsync(context, "DetallesVentas", "Descripcion", "TEXT NULL");
        await AddColumnIfMissingAsync(context, "DetallesVentas", "PrendaId", "INTEGER NULL");

        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS Apartados (Id INTEGER NOT NULL CONSTRAINT PK_Apartados PRIMARY KEY AUTOINCREMENT, NumeroApartado TEXT NOT NULL, NombreClienta TEXT NOT NULL, Telefono TEXT NOT NULL, Anticipo TEXT NOT NULL, Total TEXT NOT NULL, FechaExpiracion TEXT NOT NULL, FechaLiquidacion TEXT NULL, Estado TEXT NOT NULL, UsuarioId INTEGER NOT NULL, FechaCreacion TEXT NOT NULL, FechaModificacion TEXT NULL, Activo INTEGER NOT NULL);");
        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_Apartados_NumeroApartado ON Apartados (NumeroApartado);");
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS DetallesApartados (Id INTEGER NOT NULL CONSTRAINT PK_DetallesApartados PRIMARY KEY AUTOINCREMENT, ApartadoId INTEGER NOT NULL, PrendaId INTEGER NOT NULL, Cantidad INTEGER NOT NULL, PrecioUnitario TEXT NOT NULL, FechaCreacion TEXT NOT NULL, FechaModificacion TEXT NULL, Activo INTEGER NOT NULL);");
    }

    private static async Task EnsureNullableSaleProductAsync(MareaBravaDbContext context)
    {
        var required = await context.Database
            .SqlQueryRaw<int>("SELECT \"notnull\" AS Value FROM pragma_table_info('DetallesVentas') WHERE name = 'PrendaId'")
            .SingleOrDefaultAsync();

        if (required != 1)
            return;

        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE DetallesVentas_Reparacion (Id INTEGER NOT NULL CONSTRAINT PK_DetallesVentas_Reparacion PRIMARY KEY AUTOINCREMENT, VentaId INTEGER NOT NULL, PrendaId INTEGER NULL, Cantidad INTEGER NOT NULL, PrecioUnitario TEXT NOT NULL, Descripcion TEXT NULL, FechaCreacion TEXT NOT NULL, FechaModificacion TEXT NULL, Activo INTEGER NOT NULL);");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO DetallesVentas_Reparacion (Id, VentaId, PrendaId, Cantidad, PrecioUnitario, Descripcion, FechaCreacion, FechaModificacion, Activo) SELECT Id, VentaId, PrendaId, Cantidad, PrecioUnitario, Descripcion, FechaCreacion, FechaModificacion, Activo FROM DetallesVentas;");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE DetallesVentas;");
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE DetallesVentas_Reparacion RENAME TO DetallesVentas;");
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_DetallesVentas_VentaId ON DetallesVentas (VentaId);");
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_DetallesVentas_PrendaId ON DetallesVentas (PrendaId);");
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
    }

#pragma warning disable EF1002 // Table and column names come only from the internal schema definition above.
    private static async Task AddColumnIfMissingAsync(MareaBravaDbContext context, string table, string column, string definition)
    {
        var columns = await context.Database.SqlQueryRaw<string>($"SELECT name AS Value FROM pragma_table_info('{table}') WHERE name = '{column}'").ToListAsync();
        if (columns.Count == 0)
            await context.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition};");
    }
#pragma warning restore EF1002
}