using HoneypotTrack.Domain.Entities;

namespace HoneypotTrack.Infrastrcture.Persistences.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repositories
    IGenericRepository<Usuario> Usuarios { get; }
    IGenericRepository<Cuenta> Cuentas { get; }
    IGenericRepository<Categoria> Categorias { get; }
    IGenericRepository<Contact> Contacts { get; }
    IGenericRepository<Transaccion> Transacciones { get; }

    // Transaction methods
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
