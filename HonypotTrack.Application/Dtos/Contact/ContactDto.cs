namespace HonypotTrack.Application.Dtos.Contact;

public class ContactDto
{
    public int ContactsId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string TaxId { get; set; } = null!;

    // Info del usuario
    public string? UsuarioNombre { get; set; }
}
