using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;

namespace HoneypotTrack.Infrastrcture.Persistences.Services;

/// <summary>
/// Resuelve el DbContext correcto basado en si es una sesión honeypot o usuario legítimo.
/// 
/// Flujo:
/// - Usuario legítimo (login correcto) → AppDbContext (datos REALES)
/// - Atacante (token con claim hpt=1) → HoneypotDbContext (datos FALSOS)
/// - Auditoría → SIEMPRE AppDbContext
/// </summary>
public class DataContextProvider : IDataContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _mainContext;
    private readonly HoneypotDbContext _honeypotContext;
    private readonly ILogger<DataContextProvider> _logger;

    public DataContextProvider(
        IHttpContextAccessor httpContextAccessor,
        AppDbContext mainContext,
        HoneypotDbContext honeypotContext,
        ILogger<DataContextProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _mainContext = mainContext;
        _honeypotContext = honeypotContext;
        _logger = logger;
    }

    /// <summary>
    /// Determina si la request actual es de un atacante honeypot
    /// </summary>
    public bool IsHoneypotRequest
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return false;

            // El HoneypotMiddleware marca las requests honeypot con este item
            return context.Items.TryGetValue("IsHoneypot", out var isHoneypot) &&
                   isHoneypot is bool honeypotFlag &&
                   honeypotFlag;
        }
    }

    /// <summary>
    /// Obtiene el DbContext para operaciones de datos.
    /// - Atacante → HoneypotDbContext (datos falsos)
    /// - Usuario legítimo → AppDbContext (datos reales)
    /// </summary>
    public DbContext GetDataContext()
    {
        if (IsHoneypotRequest)
        {
            _logger.LogDebug("🍯 [DataContextProvider] Usando HoneypotDbContext - Request de atacante");
            return _honeypotContext;
        }

        _logger.LogDebug("✅ [DataContextProvider] Usando AppDbContext - Usuario legítimo");
        return _mainContext;
    }

    /// <summary>
    /// Obtiene el DbContext para auditoría - SIEMPRE es el principal
    /// </summary>
    public DbContext GetAuditContext()
    {
        return _mainContext;
    }
}
