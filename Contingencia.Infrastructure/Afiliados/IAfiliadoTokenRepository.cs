namespace Contingencia.Infrastructure.Afiliados;

public interface IAfiliadoTokenRepository
{
    // Siempre relee el token vigente antes de cada llamada al WS de afiliados (no se cachea
    // en memoria al iniciar la app) — permite rotarlo sin redeploy. Null si nunca se ha
    // configurado ninguno.
    Task<string?> ObtenerVigenteAsync();
}
