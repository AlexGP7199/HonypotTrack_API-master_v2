using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HonypotTrack.Application.Interfaces;
using HonypotTrack.Application.Services;

namespace HonypotTrack.Application.Extensions;

public static class InjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Services de Application
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<ICuentaService, CuentaService>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<ITransaccionService, TransaccionService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
