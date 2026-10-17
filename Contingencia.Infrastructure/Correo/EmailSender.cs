using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Contingencia.Infrastructure.Correo;

// Mismo mecanismo que crearcorreo() en Login3.aspx.vb (SmtpClient + configuración) — port
// exacto de Imperium.Infrastructure.Correo.EmailSender (Recobros), sin nada específico de
// ese módulo.
public class EmailSender(IConfiguration configuration) : IEmailSender
{
    public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml) =>
        EnviarInternoAsync([destinatario], asunto, cuerpoHtml, adjunto: null, nombreAdjunto: null);

    public Task EnviarAsync(IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml, byte[] adjunto, string nombreAdjunto) =>
        EnviarInternoAsync(destinatarios, asunto, cuerpoHtml, adjunto, nombreAdjunto);

    private async Task EnviarInternoAsync(
        IReadOnlyList<string> destinatarios, string asunto, string cuerpoHtml, byte[]? adjunto, string? nombreAdjunto)
    {
        var remitente = configuration["Smtp:User"]
            ?? throw new InvalidOperationException("Falta configurar 'Smtp:User'.");
        var clave = configuration["Smtp:Password"]
            ?? throw new InvalidOperationException("Falta configurar 'Smtp:Password'.");
        var host = configuration["Smtp:Host"]
            ?? throw new InvalidOperationException("Falta configurar 'Smtp:Host'.");

        using var mensaje = new MailMessage
        {
            From = new MailAddress(remitente, "DQS IMPERIUM", Encoding.UTF8),
            Subject = asunto,
            SubjectEncoding = Encoding.UTF8,
            Body = cuerpoHtml,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Priority = MailPriority.High,
        };
        foreach (var destinatario in destinatarios)
        {
            mensaje.To.Add(destinatario);
        }

        MemoryStream? adjuntoStream = null;
        if (adjunto is not null)
        {
            adjuntoStream = new MemoryStream(adjunto);
            mensaje.Attachments.Add(new Attachment(adjuntoStream, nombreAdjunto ?? "adjunto.pdf", "application/pdf"));
        }

        try
        {
            using var smtp = new SmtpClient(host, 587)
            {
                Credentials = new NetworkCredential(remitente, clave),
                EnableSsl = true,
            };

            await smtp.SendMailAsync(mensaje);
        }
        finally
        {
            adjuntoStream?.Dispose();
        }
    }
}
