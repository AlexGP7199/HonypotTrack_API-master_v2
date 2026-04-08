using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;

namespace HoneypotTrack.Infrastrcture.Persistences.Repositories;

/// <summary>
/// Unit of Work que usa IDataContextProvider para resolver el DbContext correcto.
/// - Usuario legítimo → AppDbContext (datos reales)
/// - Atacante honeypot → HoneypotDbContext (datos falsos)
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly IDataContextProvider _contextProvider;
    private IDbContextTransaction? _transaction;

    // Lazy initialization de repositorios
    private IGenericRepository<Usuario>? _usuarios;
    private IGenericRepository<Cuenta>? _cuentas;
    private IGenericRepository<Categoria>? _categorias;
    private IGenericRepository<Contact>? _contacts;
    private IGenericRepository<Transaccion>? _transacciones;

    public UnitOfWork(IDataContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
        _context = contextProvider.GetDataContext();
    }

    // Repositories - cada uno usa el DataContextProvider para resolver el contexto correcto
    public IGenericRepository<Usuario> Usuarios =>
        _usuarios ??= new GenericRepository<Usuario>(_contextProvider);

    public IGenericRepository<Cuenta> Cuentas =>
        _cuentas ??= new GenericRepository<Cuenta>(_contextProvider);

    public IGenericRepository<Categoria> Categorias =>
        _categorias ??= new GenericRepository<Categoria>(_contextProvider);

    public IGenericRepository<Contact> Contacts =>
        _contacts ??= new GenericRepository<Contact>(_contextProvider);

    public IGenericRepository<Transaccion> Transacciones =>
        _transacciones ??= new GenericRepository<Transaccion>(_contextProvider);

    // Transaction methods
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // Dispose
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
