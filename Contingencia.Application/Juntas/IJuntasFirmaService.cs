using Contingencia.Infrastructure.Juntas;

namespace Contingencia.Application.Juntas;

// Columnas reales de sp_DQS_ConsultaProfesional (DQS_MIP_PROFESIONALES). "Firma" se deja
// como el texto crudo ("Si"/"No") tal como lo compara el legado — no se traduce a bool
// para no adivinar un significado más "intuitivo" que el que realmente tiene la columna.
public record ProfesionalEncontrado(
    string Firma, string TipoDoc, string NoIdentificacion, string ApellidosYNombres,
    string Profesion, string Servicio, string CorreoElectronico, string Celular);

public record ResultadoAccionJunta(bool Exito, string Mensaje);

// Equivalente a JuntasFirma.aspx — SOLO la parte de la página que realmente se ejecuta:
// buscar profesionales, asignarlos/quitarlos del acta, y notificar por correo cuando ya
// están todos. El lienzo de captura de firma a mano alzada (canvas + WebMethod
// UploadImage) está dentro de un bloque comentado (<%-- --%>) en el propio .aspx — código
// muerto, inalcanzable desde la UI real — y no se migra (ver BASE_DE_DATOS.md).
public interface IJuntasFirmaService
{
    Task<IReadOnlyList<ProfesionalEncontrado>> BuscarProfesionalesAsync(
        string cia, string documento, string nombre, string profesion, string servicio);

    // Reutiliza el mismo sp_DQS_ConsultaProfesionalAñadido que ya usa la generación del
    // acta en PDF (ver ActaPdfBuilder) — es el mismo panel "Profesionales asignados".
    Task<IReadOnlyList<MiembroJunta>> ConsultarAsignadosAsync(string cia, string acta);

    // Réplica exacta de gvjuntas2_OnRowCommand (CommandName="ASG"), mismo orden de
    // validaciones y mismos mensajes: acta vacía/"&nbsp" → "Aún no se ha generado acta";
    // Firma="Si" → "El profesional no ha cargado la firma" (así compara el legado, tal
    // cual, aunque el nombre de la columna sugiera lo contrario); asignación fallida
    // (típicamente ya estaba asignado) → "El profesional ya ha sido asignado a esta acta".
    Task<ResultadoAccionJunta> AsignarAsync(string cia, string acta, ProfesionalEncontrado profesional);

    // Réplica de gvconsulta_OnRowCommand (CommandName="ELM").
    Task<ResultadoAccionJunta> EliminarAsync(string cia, string acta, string documento);

    // Réplica de btnnot_ServerClick: envía el correo de "Acta firmada" (destinatario fijo
    // del legado, ver JuntasFirmaService) y marca ESTADO='ENVIADA A EPS' en
    // DQS_MIP_PROFESIONALESACTA. Si el envío falla, no se marca el estado — mismo
    // comportamiento del Try/Catch original.
    Task<ResultadoAccionJunta> NotificarAsync(string cia, string acta);

    // Agregado al legado el 2026-10-01 ("enviar acta por correo", ver btnEnviarCorreo_Click
    // en JuntaProfesional.aspx.vb) — admite varios destinatarios separados por "," o ";",
    // descarta los que no son una dirección válida (mismo criterio que MailAddress). Réplica
    // de DestinatariosValidos.
    IReadOnlyList<string> ParsearDestinatariosCorreo(string texto);

    // Réplica de btnEnviarCorreo_Click (solo la parte de envío — el PDF del acta ya viene
    // generado y guardado por quien llama, igual que el legado lo genera y lo guarda ANTES
    // de intentar el envío). `destinatarios` debe venir ya validado y no vacío con
    // ParsearDestinatariosCorreo.
    Task<ResultadoAccionJunta> EnviarActaCorreoAsync(
        IReadOnlyList<string> destinatarios, string asunto, string observacion,
        string noPrescripcion, string noActa, byte[] pdfActa, string nombreArchivo);
}
