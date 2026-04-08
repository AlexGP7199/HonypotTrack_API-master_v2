using System.Reflection;
using Microsoft.OpenApi.Models;

namespace HoneypotTrack.API.Configuration;

/// <summary>
/// Configuración centralizada de Swagger/OpenAPI
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Configura los servicios de Swagger
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "HoneypotTrack API",
                Version = "v1",
                Description = "API para gestión de finanzas personales - Tracking de ingresos y egresos\n\n" +
                              "🔐 **Autenticación**: Use `/api/Auth/login` para obtener un token JWT.\n" +
                              "Luego haga clic en **Authorize** 🔓 y pegue el token.",
                Contact = new OpenApiContact
                {
                    Name = "HoneypotTrack Team",
                    Email = "support@honeypottrack.com",
                    Url = new Uri("https://github.com/AlexGP7199/HonypotTrack_API")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // 🔐 Configuración de JWT para Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Ingrese el token JWT.\n\nEjemplo: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Incluir comentarios XML para documentación
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Ordenar endpoints por tag/controller
            options.OrderActionsBy(api => api.GroupName);
        });

        return services;
    }

    /// <summary>
    /// Configura el middleware de Swagger
    /// </summary>
    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Swagger disponible en todos los ambientes
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "HoneypotTrack API v1");
            options.RoutePrefix = string.Empty; // Swagger en la raíz
            
            // Configuraciones de UI
            options.DocumentTitle = "HoneypotTrack API - Swagger";
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
            
            // Habilitar "Try it out" por defecto
            options.EnableTryItOutByDefault();
        });

        return app;
    }
}
