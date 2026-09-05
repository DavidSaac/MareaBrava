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
    public DbSet<Apartado> Apartados => Set<Apartado>();
    public DbSet<DetalleApartado> DetallesApartados => Set<DetalleApartado>();
    public DbSet<InformacionTienda> InformacionTienda => Set<InformacionTienda>();

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

        modelBuilder.Entity<InformacionTienda>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.TextoPromocional).IsRequired().HasMaxLength(2000);
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.Property(v => v.Subtotal).HasPrecision(18, 2);
            entity.Property(v => v.DescuentoPorcentaje).HasPrecision(18, 2);
            entity.Property(v => v.DescuentoMonto).HasPrecision(18, 2);
            entity.Property(v => v.EfectivoRecibido).HasPrecision(18, 2);
            entity.Property(v => v.Cambio).HasPrecision(18, 2);
            entity.Property(v => v.NotaAdministrativa).HasMaxLength(500);
        });

        modelBuilder.Entity<DetalleApartado>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            entity.HasOne(d => d.Apartado).WithMany(a => a.Detalles).HasForeignKey(d => d.ApartadoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Prenda).WithMany().HasForeignKey(d => d.PrendaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Apartado>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.NumeroApartado).IsUnique();
            entity.Property(a => a.NumeroApartado).IsRequired().HasMaxLength(50);
            entity.Property(a => a.NombreClienta).IsRequired().HasMaxLength(150);
            entity.Property(a => a.Telefono).IsRequired().HasMaxLength(30);
            entity.Property(a => a.Anticipo).HasPrecision(18, 2);
            entity.Property(a => a.Total).HasPrecision(18, 2);
            entity.Property(a => a.Estado).IsRequired().HasMaxLength(20);
            entity.HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Restrict);
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
                .IsRequired(false)
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