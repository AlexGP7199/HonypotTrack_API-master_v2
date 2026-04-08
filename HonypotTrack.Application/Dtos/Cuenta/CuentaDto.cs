namespace HonypotTrack.Application.Dtos.Cuenta;

public class CuentaDto
{
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public string AccountName { get; set; } = null!;
    public string Currency { get; set; } = "USD";

    // Info del usuario
    public string? UsuarioNombre { get; set; }
}
