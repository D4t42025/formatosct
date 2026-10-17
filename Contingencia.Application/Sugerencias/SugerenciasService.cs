using System.Net;
using Contingencia.Infrastructure.Correo;

namespace Contingencia.Application.Sugerencias;

// Equivalente a crearcorreo() en Sugerencias.aspx.vb (el archivo de la familia "Mipres",
// master SiteMIPRESN — al que ya enlazaba "PQR" en AppLayout.razor vía LegacyUrlBuilder).
// No confundir con SugerenciasREC.aspx.vb, un archivo DISTINTO que usa Recobros (Site.Master)
// — tienen destinatarios y asunto reales ligeramente distintos, confirmados leyendo cada
// fuente por separado en vez de asumir que son el mismo archivo.
public class SugerenciasService(IEmailSender emailSender) : ISugerenciasService
{
    private const string Destinatarios =
        "soporte2@dqualitysolutions.com.co,pipito1001@gmail.com,dataquality.comercial@gmail.com,dataquality.pqr@gmail.com";

    public async Task<bool> EnviarAsync(string tipo, string email, string mensaje, string usuario)
    {
        try
        {
            var cuerpo = PlantillaNotificacionSugerencia.Generar(
                WebUtility.HtmlEncode(tipo),
                WebUtility.HtmlEncode(mensaje),
                WebUtility.HtmlEncode(email),
                WebUtility.HtmlEncode(usuario));

            await emailSender.EnviarAsync(Destinatarios, "Notificación PQR Imperium-Mipres", cuerpo);
            return true;
        }
        catch
        {
            // Igual que el legado: un fallo de SMTP se atrapa y se muestra "No es posible
            // enviar el mensaje" en vez de propagar la excepción.
            return false;
        }
    }
}
