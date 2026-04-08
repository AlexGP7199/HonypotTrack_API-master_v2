namespace HoneypotTrack.Domain.Entities;

/// <summary>
/// Tarjeta de crédito falsa (solo para honeypot)
/// Datos atractivos para atacantes
/// </summary>
public class TarjetaCredito
{
    public int TarjetaId { get; set; }
    public int UsuarioId { get; set; }
    public string NumeroTarjeta { get; set; } = null!;
    public string CVV { get; set; } = null!;
    public string FechaExpiracion { get; set; } = null!;
    public string NombreTitular { get; set; } = null!;
    public decimal? LimiteCredito { get; set; }
    public decimal? SaldoActual { get; set; }
    public string? TipoTarjeta { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Navigation
    public virtual Usuario? Usuario { get; set; }
}
