using System.Net;

namespace Contingencia.Infrastructure.Correo;

// Versión modernizada/simplificada de CuerpoDelCorreo en JuntaProfesional.aspx.vb (agregado
// al legado el 2026-10-01, función "enviar acta por correo") — mismo mensaje real, mismo
// criterio que PlantillaActaFirmada: sin el boilerplate de compatibilidad con Outlook que no
// hace falta para un correo tan simple.
public static class PlantillaActaCorreo
{
    public static string Generar(string prescripcion, string acta, string observacion)
    {
        // La observación la escribe el usuario en un <textarea> — va codificada porque el
        // correo es HTML y no puede aceptar etiquetas que vengan del formulario (réplica del
        // Server.HtmlEncode del legado).
        var observacionHtml = WebUtility.HtmlEncode(observacion).Replace("\r\n", "<br />").Replace("\n", "<br />");

        var bloqueObservacion = observacion.Length == 0
            ? ""
            : $"<p><b>Observación:</b></p><p>{observacionHtml}</p>";

        return $"""
            <div style="font-family:'Trebuchet MS',Arial,sans-serif;font-size:14px;color:#2c3e50;">
              <p>Se adjunta el acta de la junta profesional.</p>
              <table cellpadding="6" cellspacing="0" style="border-collapse:collapse;margin:12px 0;">
                <tr><td style="background:#f1f5fa;"><b>Prescripción</b></td><td>{WebUtility.HtmlEncode(prescripcion)}</td></tr>
                <tr><td style="background:#f1f5fa;"><b>Acta</b></td><td>{WebUtility.HtmlEncode(acta)}</td></tr>
              </table>
              {bloqueObservacion}
              <p style="color:#7b8794;font-size:12px;">Mensaje generado por DQS IMPERIUM.</p>
            </div>
            """;
    }
}
