namespace Contingencia.Infrastructure.Mipres;

public interface IMipresPrescripcionValidator
{
    Task<bool> EsValidaAsync(string cia, string noPrescripcion);
}
