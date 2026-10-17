using System.Data;
using Contingencia.Infrastructure.Correo;
using Contingencia.Infrastructure.Juntas;

namespace Contingencia.Application.Juntas;

public class JuntasFirmaService(IJuntasRepository repository, IEmailSender emailSender) : IJuntasFirmaService
{
    // Destinatario fijo tomado literal de btnnot_ServerClick en JuntasFirma.aspx.vb — una
    // dirección personal (@gmail.com), no una cuenta de rol de EPS Sanitas. Es una
    // decisión de negocio del legado, no un error de código; se replica tal cual (a
    // diferencia de un pie de correo con marca ajena, cambiar el destinatario real de una
    // notificación operativa no es algo que esta migración deba decidir por su cuenta —
    // ver BASE_DE_DATOS.md).
    private const string DestinatarioNotificacion = "fmayorga1001@gmail.com";

    public async Task<IReadOnlyList<ProfesionalEncontrado>> BuscarProfesionalesAsync(
        string cia, string documento, string nombre, string profesion, string servicio)
    {
        var dt = await repository.BuscarProfesionalesAsync(cia, documento, nombre, profesion, servicio);
        return dt.AsEnumerable()
            .Select(f => new ProfesionalEncontrado(
                Convert.ToString(f["Firma"]) ?? "",
                Convert.ToString(f["TIPODOC"]) ?? "",
                Convert.ToString(f["NoIDENTIFICACION"]) ?? "",
                Convert.ToString(f["APELLIDOS Y NOMBRES"]) ?? "",
                Convert.ToString(f["PROFESION"]) ?? "",
                Convert.ToString(f["SERVICIO"]) ?? "",
                Convert.ToString(f["CORREOELECTRONICO"]) ?? "",
                Convert.ToString(f["CELULAR"]) ?? ""))
            .ToList();
    }

    public async Task<IReadOnlyList<MiembroJunta>> ConsultarAsignadosAsync(string cia, string acta)
    {
        var dt = await repository.ConsultarProfesionalesActaAsync(cia, acta);
        return dt.AsEnumerable()
            .Select(f => new MiembroJunta(
                Convert.ToString(f["NoIDENTIFICACION"]) ?? "",
                Convert.ToString(f["APELLIDOS Y NOMBRES"]) ?? "",
                Convert.ToString(f["PROFESION"]) ?? "",
                // Esta lista solo se muestra en la tabla "Profesionales asignados" del modal
                // Firma — no arma un PDF, así que no vale la pena descargar la imagen acá.
                ImagenFirma: null))
            .ToList();
    }

    public async Task<ResultadoAccionJunta> AsignarAsync(string cia, string acta, ProfesionalEncontrado profesional)
    {
        if (string.IsNullOrWhiteSpace(acta) || acta == "&nbsp")
        {
            return new ResultadoAccionJunta(false, "Aún no se ha generado acta");
        }

        if (profesional.Firma == "Si")
        {
            return new ResultadoAccionJunta(false, "El profesional no ha cargado la firma");
        }

        var asignado = await repository.AsignarProfesionalAsync(cia, acta, profesional.NoIdentificacion);
        return asignado
            ? new ResultadoAccionJunta(true, "Profesional añadido")
            : new ResultadoAccionJunta(false, "El profesional ya ha sido asignado a esta acta");
    }

    public async Task<ResultadoAccionJunta> EliminarAsync(string cia, string acta, string documento)
    {
        var eliminado = await repository.EliminarProfesionalAsync(cia, acta, documento);
        return eliminado
            ? new ResultadoAccionJunta(true, "Profesional eliminado")
            : new ResultadoAccionJunta(false, "No se ha podido eliminar el profesional del acta");
    }

    public async Task<ResultadoAccionJunta> NotificarAsync(string cia, string acta)
    {
        try
        {
            var cuerpo = PlantillaActaFirmada.Generar(acta);
            await emailSender.EnviarAsync(DestinatarioNotificacion, "ACTA FIRMADA POR TODA LA JUNTA PROFESIONAL", cuerpo);
            await repository.ActualizarEstadoActaAsync(cia, acta);
            return new ResultadoAccionJunta(true, "Notificación realizada");
        }
        catch
        {
            // Réplica fiel: si el envío falla, el legado tampoco marca el estado (el
            // ActualizaEstadoActaAsync vive dentro del mismo Try, después del envío).
            return new ResultadoAccionJunta(false, "El mensaje no fue posible enviarlo");
        }
    }

    public IReadOnlyList<string> ParsearDestinatariosCorreo(string texto)
    {
        var validos = new List<string>();

        foreach (var parte in texto.Split([';', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            var limpio = parte.Trim();
            if (limpio.Length == 0)
            {
                continue;
            }

            try
            {
                // Es el mismo analizador que usará MailMessage al enviar: si aquí pasa, allá
                // también (réplica de DestinatariosValidos).
                var direccion = new System.Net.Mail.MailAddress(limpio);
                validos.Add(direccion.Address);
            }
            catch (FormatException)
            {
                // Una dirección mal escrita no debe tumbar el envío a las demás.
            }
        }

        return validos;
    }

    public async Task<ResultadoAccionJunta> EnviarActaCorreoAsync(
        IReadOnlyList<string> destinatarios, string asunto, string observacion,
        string noPrescripcion, string noActa, byte[] pdfActa, string nombreArchivo)
    {
        try
        {
            var cuerpo = PlantillaActaCorreo.Generar(noPrescripcion, noActa, observacion);
            await emailSender.EnviarAsync(destinatarios, asunto, cuerpo, pdfActa, nombreArchivo);
            return new ResultadoAccionJunta(true, $"El acta se envió a {string.Join(", ", destinatarios)}.");
        }
        catch
        {
            // Réplica fiel: si el envío falla, el acta ya quedó generada y guardada (eso lo
            // hizo quien llamó a este método, antes de llegar acá) — el mensaje se lo dice al
            // usuario explícitamente, igual que el legado.
            return new ResultadoAccionJunta(false, "El acta quedó guardada, pero el correo no salió.");
        }
    }
}
