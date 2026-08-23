using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;

namespace MareaBrava.Infrastructure.Data;

public class MareaBravaDbContext : DbContext
{
    public MareaBravaDbContext(DbContextOptions<MareaBravaDbContext> options) : base(options)
    {
    }

    public DbSet<Prenda> Prendas => Set<Prenda>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Índices únicos y configuraciones de negocio
        modelBuilder.Entity<Prenda>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configuración de precisión decimal para monedas
        modelBuilder.Entity<Prenda>()
            .Property(p => p.PrecioCosto)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Prenda>()
            .Property(p => p.PrecioVenta)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Venta>()
            .Property(v => v.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DetalleVenta>()
            .Property(d => d.PrecioUnitario)
            .HasPrecision(18, 2);
    }
}