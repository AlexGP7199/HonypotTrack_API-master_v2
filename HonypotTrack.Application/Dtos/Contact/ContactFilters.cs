using HonypotTrack.Application.Commons.Bases;

namespace HonypotTrack.Application.Dtos.Contact;

public class ContactFilters : BaseFilters
{
    public int? UserId { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; } // Cliente, Proveedor, Persona
    public string? TaxId { get; set; }
}
