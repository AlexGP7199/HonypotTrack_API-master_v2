using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoneypotTrack.Domain.Base;

namespace HoneypotTrack.Domain.Entities;

[Table("Usuarios", Schema = "empresa")]
public class Usuario : BaseEntity
{
    [Key]
    [Column("Userid")]
    public int UserId { get; set; }

    [Column("fullname")]
    [StringLength(100)]
    public string? FullName { get; set; }

    [Column("email")]
    [StringLength(100)]
    [Required]
    public string Email { get; set; } = null!;

    [Column("passwordhash")]
    [StringLength(256)]
    public string? PasswordHash { get; set; }

    [Column("refreshtoken")]
    [StringLength(256)]
    public string? RefreshToken { get; set; }

    [Column("refreshtokenexpiry")]
    public DateTime? RefreshTokenExpiry { get; set; }

    // Propiedades de navegación
    public virtual ICollection<Cuenta> Cuentas { get; set; } = [];
    public virtual ICollection<Contact> Contacts { get; set; } = [];
}
