using Microsoft.EntityFrameworkCore;
using MareaBrava.Domain.Entities;

namespace MareaBrava.Infrastructure.Data;

public class MareaBravaDbContext : DbContext
{
    public MareaBravaDbContext(DbContextOptions<MareaBravaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Prenda> Prendas => Set<Prenda>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVentas => Set<DetalleVenta>();
    public DbSet<CorteCaja> CortesCaja => Set<CorteCaja>();
    public DbSet<CategoriaProducto> CategoriasProductos => Set<CategoriaProducto>();
    public DbSet<Gasto> Gastos => Set<Gasto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Prenda
        modelBuilder.Entity<Prenda>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Color).IsRequired().HasMaxLength(50);
            entity.Property(p => p.PrecioCosto).HasPrecision(18, 2);
            entity.Property(p => p.PrecioVenta).HasPrecision(18, 2);
            entity.HasOne(p => p.Categoria)
                  .WithMany(c => c.Prendas)
                  .HasForeignKey(p => p.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CategoriaProducto>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Nombre).IsUnique();
        });

        modelBuilder.Entity<Gasto>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Concepto).IsRequired().HasMaxLength(150);
            entity.Property(g => g.Monto).HasPrecision(18, 2);
            entity.Property(g => g.Observaciones).HasMaxLength(500);
        });

        // Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        // Venta
        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.NumeroTicket).IsRequired().HasMaxLength(50);
            entity.Property(v => v.Total).HasPrecision(18, 2);

            entity.HasOne(v => v.Usuario)
                  .WithMany()
                  .HasForeignKey(v => v.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // DetalleVenta
        modelBuilder.Entity<DetalleVenta>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            entity.Ignore(d => d.Subtotal);

            entity.HasOne(d => d.Venta)
                  .WithMany(v => v.Detalles)
                  .HasForeignKey(d => d.VentaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Prenda)
                  .WithMany()
                  .HasForeignKey(d => d.PrendaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CorteCaja
        modelBuilder.Entity<CorteCaja>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FondoInicial).HasPrecision(18, 2);
            entity.Property(c => c.VentasEfectivo).HasPrecision(18, 2);
            entity.Property(c => c.VentasTarjeta).HasPrecision(18, 2);
            entity.Property(c => c.VentasTransferencia).HasPrecision(18, 2);
            entity.Property(c => c.TotalEsperadoEfectivo).HasPrecision(18, 2);
            entity.Property(c => c.EfectivoRealContado).HasPrecision(18, 2);
            entity.Property(c => c.Diferencia).HasPrecision(18, 2);

            entity.HasOne(c => c.Usuario)
                  .WithMany()
                  .HasForeignKey(c => c.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}