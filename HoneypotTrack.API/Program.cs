using HonypotTrack.Application.Extensions;
using HoneypotTrack.Infrastrcture.Extensions;
using HonypotTrack.Application.Helpers;
using HoneypotTrack.API.Middlewares;
using HoneypotTrack.API.Security;
using HoneypotTrack.API.Configuration;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Infrastructure: DbContext, Repositories, UnitOfWork
// - AppDbContext (BD Principal - datos reales)
// - HoneypotDbContext (BD Se�uelo - datos falsos para atacantes)
// - DataContextProvider (resuelve qu� DB usar seg�n el tipo de request)
builder.Services.AddInfrastructure(builder.Configuration);

// Application: Services, DTOs, AutoMapper
builder.Services.AddApplication(builder.Configuration);

// ?? Security Alert Service
builder.Services.AddSingleton<ISecurityAlertService, SecurityAlertService>();

// ?? Honeypot Session Service
builder.Services.AddScoped<IHoneypotSessionService, HoneypotSessionService>();
builder.Services.AddSingleton<ILoginAttemptTracker, LoginAttemptTracker>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "HoneypotTrack_SuperSecretKey_2024_MinLength32Chars!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "HoneypotTrackAPI",
        ValidAudience = jwtSettings["Audience"] ?? "HoneypotTrackClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS: Permitir todas las conexiones
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Pol�tica espec�fica para producci�n (opcional)
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:4200",
                "http://localhost:5173",
                "https://tudominio.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Session para tracking
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllers();

// ?? Swagger / OpenAPI con configuraci�n de JWT
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

// Inicializar AutoMapper (sin DI)
_ = AutoMapperHelper.Instance;

// Configure the HTTP request pipeline.
// ?? Swagger UI con configuraci�n de JWT (Authorize button)
app.UseSwaggerConfiguration(app.Environment);

// CORS - debe ir antes de otros middlewares
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Session
app.UseSession();

// ?? Middleware de Honeypot (detecta tokens honeypot y registra actividad)
app.UseHoneypotMiddleware();

// ??? Middleware de seguridad OWASP (detecta amenazas y activa honeypot si es necesario)
app.UseSecurityMiddleware();

// Middleware de auditor�a personalizado
app.UseAuditMiddleware();

app.MapControllers();

app.Run();
