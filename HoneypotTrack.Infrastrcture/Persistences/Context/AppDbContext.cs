using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;

namespace HoneypotTrack.Infrastrcture.Persistences.Context;

public partial class AppDbContext : DbContext
{
    private readonly IAuditoriaContext? _auditoriaContext;

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, IAuditoriaContext auditoriaContext)
        : base(options)
    {
        _auditoriaContext = auditoriaContext;
    }

    // DbSets
    public virtual DbSet<Usuario> Usuarios { get; set; } = null!;
    public virtual DbSet<Cuenta> Cuentas { get; set; } = null!;
    public virtual DbSet<Categoria> Categorias { get; set; } = null!;
    public virtual DbSet<Contact> Contacts { get; set; } = null!;
    public virtual DbSet<Transaccion> Transacciones { get; set; } = null!;
    public virtual DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public virtual DbSet<AuditoriaEntidad> AuditoriaEntidades { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditorias = new List<AuditoriaEntidad>();

        var entradas = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || 
                        e.State == EntityState.Modified || 
                        e.State == EntityState.Deleted)
            .Where(e => e.Entity is not AuditoriaEntidad && e.Entity is not AuditLog); // Excluir tablas de auditoría

        foreach (var entry in entradas)
        {
            var entidad = entry.Entity;
            var tipo = entidad.GetType().Name;

            string? pk = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?
                .CurrentValue?.ToString();

            var auditoria = new AuditoriaEntidad
            {
                NombreTabla = tipo,
                LlavePrimaria = pk ?? "N/A",
                Accion = entry.State.ToString(),
                FechaCambio = DateTime.UtcNow,
                FechaCambioLocal = DateTime.Now,
                UsuarioId = _auditoriaContext?.GetUsuarioId(),
                UsuarioNombre = _auditoriaContext?.GetUsuarioNombre(),
                UsuarioEmail = _auditoriaContext?.GetUsuarioEmail(),
                IpAddress = _auditoriaContext?.GetIpAddress(),
                Path = _auditoriaContext?.GetPath(),
                MetodoHttp = _auditoriaContext?.GetMetodoHttp(),
                UserAgent = _auditoriaContext?.GetUserAgent(),
                CorrelationId = _auditoriaContext?.GetCorrelationId()
            };

            if (entry.State == EntityState.Modified)
            {
                var cambios = entry.Properties
                    .Where(p => p.IsModified)
                    .ToDictionary(
                        p => p.Metadata.Name,
                        p => new { Antes = p.OriginalValue?.ToString(), Despues = p.CurrentValue?.ToString() });

                auditoria.ValorAnterior = JsonSerializer.Serialize(
                    cambios.ToDictionary(c => c.Key, c => c.Value.Antes));
                auditoria.ValorNuevo = JsonSerializer.Serialize(
                    cambios.ToDictionary(c => c.Key, c => c.Value.Despues));
                auditoria.ColumnasCambiadas = string.Join(", ", cambios.Keys);
            }
            else if (entry.State == EntityState.Added)
            {
                auditoria.ValorNuevo = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString()));
            }
            else if (entry.State == EntityState.Deleted)
            {
                auditoria.ValorAnterior = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString()));
            }

            auditorias.Add(auditoria);
        }

        if (auditorias.Count > 0)
        {
            await AuditoriaEntidades.AddRangeAsync(auditorias, cancellationToken);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasMany(u => u.Cuentas)
                  .WithOne(c => c.Usuario)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.Contacts)
                  .WithOne(c => c.Usuario)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Cuenta
        modelBuilder.Entity<Cuenta>(entity =>
        {
            entity.HasMany(c => c.Transacciones)
                  .WithOne(t => t.Cuenta)
                  .HasForeignKey(t => t.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Categoria
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasMany(c => c.Transacciones)
                  .WithOne(t => t.Categoria)
                  .HasForeignKey(t => t.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Contact
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasMany(c => c.Transacciones)
                  .WithOne(t => t.Contact)
                  .HasForeignKey(t => t.ContactsId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Transaccion
        modelBuilder.Entity<Transaccion>(entity =>
        {
            entity.Property(t => t.Monto)
                  .HasColumnType("decimal(18,2)");
        });

        // AuditLog (auditoría de requests HTTP)
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.HttpMethod);
            entity.HasIndex(e => e.RequestPath);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.ActionType);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.CorrelationId);
        });

        // AuditoriaEntidad (auditoría de cambios en entidades)
        modelBuilder.Entity<AuditoriaEntidad>(entity =>
        {
            entity.HasIndex(e => e.NombreTabla);
            entity.HasIndex(e => e.LlavePrimaria);
            entity.HasIndex(e => e.Accion);
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.FechaCambio);
            entity.HasIndex(e => e.CorrelationId);
        });
    }

    // Nota: El logging de sesiones honeypot se maneja en HoneypotSessionService
}

