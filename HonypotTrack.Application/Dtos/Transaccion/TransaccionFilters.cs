using HonypotTrack.Application.Commons.Bases;

namespace HonypotTrack.Application.Dtos.Transaccion;

public class TransaccionFilters : BaseFilters
{
    public int? AccountId { get; set; }
    public int? CategoryId { get; set; }
    public int? ContactsId { get; set; }
    public string? Moneda { get; set; }
    public string? TipoOperacion { get; set; } // Ingreso o Egreso
    public decimal? MontoMinimo { get; set; }
    public decimal? MontoMaximo { get; set; }
}
