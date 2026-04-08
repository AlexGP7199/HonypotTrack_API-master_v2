using HonypotTrack.Application.Commons.Bases;

namespace HonypotTrack.Application.Dtos.Cuenta;

public class CuentaFilters : BaseFilters
{
    public int? UserId { get; set; }
    public string? AccountName { get; set; }
    public string? Currency { get; set; }
}
