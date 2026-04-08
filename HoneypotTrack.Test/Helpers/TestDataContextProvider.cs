using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;

namespace HoneypotTrack.Test.Helpers;

/// <summary>
/// Mock de IDataContextProvider para pruebas unitarias.
/// Siempre devuelve el AppDbContext (sin lógica de honeypot en tests).
/// </summary>
public class TestDataContextProvider : IDataContextProvider
{
    private readonly AppDbContext _context;

    public TestDataContextProvider(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// En tests, nunca es honeypot request
    /// </summary>
    public bool IsHoneypotRequest => false;

    /// <summary>
    /// Devuelve el contexto de prueba
    /// </summary>
    public DbContext GetDataContext() => _context;

    /// <summary>
    /// Devuelve el mismo contexto para auditoría en tests
    /// </summary>
    public DbContext GetAuditContext() => _context;
}
