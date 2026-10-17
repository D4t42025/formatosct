namespace Contingencia.Infrastructure.Captcha;

public interface ITurnstileValidator
{
    Task<bool> EsValidoAsync(string respuestaCliente);
}
