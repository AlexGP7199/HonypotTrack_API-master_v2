using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;
using HoneypotTrack.Infrastrcture.Persistences.Repositories;
using HoneypotTrack.Infrastrcture.Persistences.Services;
using HoneypotTrack.Infrastrcture.Services;

namespace HoneypotTrack.Infrastrcture.Extensions;

public static class InjectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // HttpContextAccessor (necesario para IAuditoriaContext y IDataContextProvider)
        services.AddHttpContextAccessor();

        // Auditoría Context Service
        services.AddScoped<IAuditoriaContext, AuditoriaContextService>();

        // DbContext Principal (datos reales)
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        // DbContext Honeypot (datos falsos para atacantes)
        services.AddDbContext<HoneypotDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("HoneypotConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        // Data Context Provider - resuelve qué DbContext usar según el tipo de request
        // Usuario legítimo → AppDbContext | Atacante honeypot → HoneypotDbContext
        services.AddScoped<IDataContextProvider, DataContextProvider>();

        // Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
