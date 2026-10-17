namespace Contingencia.Infrastructure.Correo;

public interface IEmailSender
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml);

    // Varios destinatarios + adjunto — usado por "enviar acta por correo" de Junta
    // Profesional (ver btnEnviarCorreo_Click en JuntaProfesional.aspx.vb, agregado al
    // legado el 2026-10-01).
    Task EnviarAsync(IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml, byte[] adjunto, string nombreAdjunto);
}
