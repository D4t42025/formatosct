using System.Data;

namespace Contingencia.Infrastructure.Juntas;

public interface IJuntasRepository
{
    // sp_DQS_ConsultaProfesionalAñadido(@CIA, @ACTA): join DQS_MIP_PROFESIONALESACTA +
    // DQS_MIP_PROFESIONALES por CIA+NoIDENTIFICACION, filtrado por CIA+ACTA. Devuelve
    // ACTA, NoIDENTIFICACION, "APELLIDOS Y NOMBRES", PROFESION — mismos nombres de columna
    // que lee gvconsulta_OnRowCommand (consultaprofesionalañadido en el legado) para armar
    // la tabla "INTEGRANTES DE LA JUNTA PROFESIONAL DE LA SALUD" del acta en PDF, y también
    // el panel "Profesionales asignados" del modal de Firma (JuntasFirma.aspx).
    Task<DataTable> ConsultarProfesionalesActaAsync(string cia, string acta);

    // sp_DQS_ConsultaProfesional(@cia, @doc, @nom, @prof, @sede, @serv) contra
    // DQS_MIP_PROFESIONALES — LIKE parcial en cada campo, "" equivale a "todos". Devuelve
    // Firma, TIPODOC, NoIDENTIFICACION, "APELLIDOS Y NOMBRES", PROFESION, SEDE, SERVICIO,
    // CORREOELECTRONICO, CELULAR, FActivación.
    //
    // Réplica de un bug real del legado (btnconsultrajuntas_ServerClick), confirmado
    // leyendo el código exacto: el parámetro @sede recibe lo que el usuario escribe en el
    // campo "Servicio" (txtserv.Value), y @serv recibe otra vez el valor de "Nombre"
    // (txtnom.Value) — el campo "Sede" del formulario (txtsede) nunca se envía al SP. Se
    // replica tal cual: el parámetro `servicio` de este método se manda como @sede, y
    // `nombre` se repite como @serv. No se expone un filtro de "Sede" en la UI migrada
    // porque nunca tuvo efecto real en el legado (ver BASE_DE_DATOS.md).
    Task<DataTable> BuscarProfesionalesAsync(string cia, string documento, string nombre, string profesion, string servicio);

    // sp_DQS_AsignaProfesional(@CIA, @ACTA, @DOC): INSERT en DQS_MIP_PROFESIONALESACTA con
    // ESTADO='APROBADA Y FIRMADA' fijo. Sin índice único conocido que documente el motivo
    // exacto, pero el legado interpreta cualquier excepción como "ya fue asignado" — se
    // replica esa misma interpretación (false = ya estaba asignado o falló).
    Task<bool> AsignarProfesionalAsync(string cia, string acta, string documento);

    // sp_DQS_EliminaProfesional(@CIA, @ACTA, @DOC): DELETE en DQS_MIP_PROFESIONALESACTA.
    Task<bool> EliminarProfesionalAsync(string cia, string acta, string documento);

    // sp_DQS_ActualizaProfesional(@CIA, @ACTA): UPDATE DQS_MIP_PROFESIONALESACTA
    // SET ESTADO='ENVIADA A EPS' — se llama tras notificar por correo que el acta ya fue
    // firmada por toda la junta.
    Task ActualizarEstadoActaAsync(string cia, string acta);

    // sp_DQS_ActualizaJunta(@Cia, @MIPRES, @TIPOT, @CONT): DELETE + INSERT en
    // DQS_MIP_JUNTASC (registro de qué prescripción/tipo tecnología/consecutivo tiene
    // documentos cargados) — se llama una vez por cada archivo subido exitosamente en
    // JuntasCargueDoc.aspx.
    Task RegistrarCargueAsync(string cia, string prescripcion, string tipoTecnologia, string consecutivo);
}
