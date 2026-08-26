using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MareaBrava.Infrastructure.Data;

public sealed class ApartadoExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ApartadoExpirationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExpireReservationsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ExpireReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MareaBravaDbContext>();
        var expired = await context.Apartados
            .Include(a => a.Detalles)
            .Where(a => a.Activo && a.Estado == "Activo" && a.FechaExpiracion <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var apartado in expired)
        {
            foreach (var detalle in apartado.Detalles)
            {
                var prenda = await context.Prendas.FindAsync(new object[] { detalle.PrendaId }, cancellationToken);
                if (prenda != null) prenda.StockActual += detalle.Cantidad;
            }
            apartado.Estado = "Expirado";
            apartado.Activo = false;
            apartado.FechaModificacion = DateTime.UtcNow;
        }

        if (expired.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}
