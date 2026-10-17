namespace Contingencia.Application.Login;

public interface ILoginService
{
    Task<ResultadoIntentoLogin> IniciarSesionAsync(string usuario, string clave, string captchaResponse, string ip);

    // Añadido por decisión explícita del usuario (2026-09-02): Login3.aspx.vb SÍ tiene esta
    // lógica (crearcorreo(), disparada por un query string "txtemail" no alcanzable desde
    // el formulario actual de Login3.aspx) — se implementa aquí como un flujo real y
    // accesible desde la UI, igual que ya lo hace Recobros.
    Task<bool> EnviarClaveTemporalAsync(string email);
}
