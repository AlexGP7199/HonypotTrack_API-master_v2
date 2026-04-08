using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Domain.Entities;

namespace HoneypotTrack.Infrastrcture.Persistences.Context;

/// <summary>
/// DbContext para la base de datos Honeypot (datos falsos para atrapar atacantes)
/// </summary>
public class HoneypotDbContext : DbContext
{
    public HoneypotDbContext(DbContextOptions<HoneypotDbContext> options) : base(options)
    {
    }

    // Entidades principales (misma estructura que la BD real)
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Cuenta> Cuentas { get; set; } = null!;
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Transaccion> Transacciones { get; set; } = null!;
    public DbSet<Contact> Contacts { get; set; } = null!;

    // Entidades especiales del honeypot
    public DbSet<TarjetaCredito> TarjetasCredito { get; set; } = null!;
    public DbSet<ApiCredential> ApiCredentials { get; set; } = null!;
    public DbSet<HoneypotSession> HoneypotSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar schema
        modelBuilder.HasDefaultSchema("empresa");

        // Usuario - usa las configuraciones de Data Annotations de la entidad
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(e => e.UserId);
        });

        // Cuenta
        modelBuilder.Entity<Cuenta>(entity =>
        {
            entity.ToTable("Cuenta");
            entity.HasKey(e => e.AccountId);
            entity.HasOne(c => c.Usuario)
                  .WithMany(u => u.Cuentas)
                  .HasForeignKey(c => c.UserId);
        });

        // Categoria
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categoria");
            entity.HasKey(e => e.CategoryId);
        });

        // Transaccion
        modelBuilder.Entity<Transaccion>(entity =>
        {
            entity.ToTable("transacciones");
            entity.HasKey(e => e.TransaccionId);
            entity.HasOne(t => t.Cuenta)
                  .WithMany(c => c.Transacciones)
                  .HasForeignKey(t => t.AccountId);
        });

        // Contact
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contacts");
            entity.HasKey(e => e.ContactsId);
        });

        // TarjetaCredito (solo en honeypot)
        modelBuilder.Entity<TarjetaCredito>(entity =>
        {
            entity.ToTable("TarjetasCredito");
            entity.HasKey(e => e.TarjetaId);
        });

        // ApiCredential (solo en honeypot)
        modelBuilder.Entity<ApiCredential>(entity =>
        {
            entity.ToTable("ApiCredentials");
            entity.HasKey(e => e.CredentialId);
        });

        // HoneypotSession
        modelBuilder.Entity<HoneypotSession>(entity =>
        {
            entity.ToTable("HoneypotSessions");
            entity.HasKey(e => e.SessionId);
        });
    }
}
