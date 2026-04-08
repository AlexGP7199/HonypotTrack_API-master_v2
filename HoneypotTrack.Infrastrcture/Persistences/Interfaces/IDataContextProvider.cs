using Microsoft.EntityFrameworkCore;

namespace HoneypotTrack.Infrastrcture.Persistences.Interfaces;

/// <summary>
/// Interface para resolver el DbContext correcto basado en el contexto de la request.
/// - Usuario legítimo → AppDbContext (datos reales)
/// - Atacante honeypot → HoneypotDbContext (datos falsos)
/// </summary>
public interface IDataContextProvider
{
    /// <summary>
    /// Obtiene el DbContext para operaciones de datos (puede ser real o honeypot)
    /// </summary>
    DbContext GetDataContext();

    /// <summary>
    /// Obtiene el DbContext principal para auditoría (SIEMPRE es el real)
    /// </summary>
    DbContext GetAuditContext();

    /// <summary>
    /// Indica si la request actual es de una sesión honeypot
    /// </summary>
    bool IsHoneypotRequest { get; }
}
