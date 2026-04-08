namespace HoneypotTrack.Domain.Entities;

/// <summary>
/// Credenciales de API falsas (solo para honeypot)
/// Datos atractivos para atacantes
/// </summary>
public class ApiCredential
{
    public int CredentialId { get; set; }
    public int UsuarioId { get; set; }
    public string ServiceName { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string? ApiSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Endpoint { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Navigation
    public virtual Usuario? Usuario { get; set; }
}
