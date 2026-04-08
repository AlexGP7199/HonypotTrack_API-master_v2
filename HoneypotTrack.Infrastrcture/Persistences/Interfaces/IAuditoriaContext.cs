namespace HoneypotTrack.Infrastrcture.Persistences.Interfaces;

public interface IAuditoriaContext
{
    int? GetUsuarioId();
    string? GetUsuarioNombre();
    string? GetUsuarioEmail();
    string? GetIpAddress();
    string? GetPath();
    string? GetMetodoHttp();
    string? GetUserAgent();
    string? GetCorrelationId();
}
