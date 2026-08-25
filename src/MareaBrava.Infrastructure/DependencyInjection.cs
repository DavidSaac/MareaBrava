using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MareaBrava.Application.Interfaces;
using MareaBrava.Application.Services;
using MareaBrava.Infrastructure.Data;
using MareaBrava.Infrastructure.Repositories;

namespace MareaBrava.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró DefaultConnection.");

        services.AddDbContext<MareaBravaDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IPrendaRepository, PrendaRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<IPrendaService, PrendaService>();
        services.AddScoped<IVentaService, VentaService>();

        return services;
    }
}
