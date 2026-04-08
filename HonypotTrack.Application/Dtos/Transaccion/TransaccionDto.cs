namespace HonypotTrack.Application.Dtos.Transaccion;

public class TransaccionDto
{
    public int TransaccionId { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; }
    public int? ContactsId { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public string? Descripcion { get; set; }
    public DateTime? Fecha { get; set; }

    // Info relacionada
    public string? CuentaNombre { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? ContactoNombre { get; set; }
    public string? TipoOperacion { get; set; }
}
