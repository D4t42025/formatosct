namespace Contingencia.Application.Login;

public enum EstadoIntentoLogin
{
    CaptchaInvalido,
    CredencialesInvalidas,
    Bloqueado,
    Exito,
}

public class ResultadoIntentoLogin
{
    public required EstadoIntentoLogin Estado { get; init; }
    public string? Mensaje { get; init; }
    public Guid? TokenPuente { get; init; }
    public string? UrlDestino { get; init; }
}
