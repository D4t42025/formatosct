using System.Data;

namespace Contingencia.Infrastructure.Contingencias;

public interface IContingenciasRepository
{
    // Los parámetros van en el mismo orden que sp_MIP_InsertaContingencia (ver
    // Contingencia.Infrastructure/Sql/010_MipContingenciasMultiplesTecnologias.sql) — el
    // executor deriva los parámetros del SP por posición, no por nombre. Las tecnologías
    // viajan como un arreglo JSON y el SP las inserta junto con la cabecera en una sola
    // transacción.
    Task<string> InsertarAsync(
        string cia, string usuario, string causal, string? causalNombre, string? causalDescripcion,
        string? ipsCodigo, string? ipsNombre,
        string profesionalDocumento, string? profesionalNombre, string? profesionalProfesion,
        string pacienteTipoDoc, string pacienteNumDoc, string pacienteNombres, string pacienteApellidos,
        string? pacienteSexo, DateTime? pacienteFechaNacimiento, string? pacienteMunicipio,
        string? pacienteTelefono, string? pacienteRegimen,
        DateTime? pacienteFechaAtencion, string? pacienteHoraAtencion,
        string? pacienteAmbitoAtencionCodigo, string? pacienteAmbitoAtencionNombre,
        string diagnosticoCodigo, string? diagnosticoDescripcion,
        string? diagnosticoRel1Codigo, string? diagnosticoRel1Descripcion,
        string? diagnosticoRel2Codigo, string? diagnosticoRel2Descripcion,
        string tecnologiasJson);

    Task<bool> ActualizarArchivosAsync(string cia, string radicado, string? firmaRuta, string? soportesRutasJson);

    Task<DataTable> ConsultarAsync(
        string cia, string? usuario, string? radicado, string? causal, string? ipsCodigo,
        string? profesionalDocumento, string? pacienteNumDoc, string? tipoTecnologia,
        DateTime? fechaInicio, DateTime? fechaFin);

    // Tecnologías (una fila por tecnología, ordenadas por radicado y ORDEN) de los radicados
    // indicados, que viajan como arreglo JSON.
    Task<DataTable> ConsultarTecnologiasAsync(string cia, string radicadosJson);

    // sp_MIP_EliminaContingencia (ver Sql/015_MipEliminaContingencia.sql) — borra en
    // transacción DQS_MIP_CONTINGENCIAS_TEC y DQS_MIP_CONTINGENCIAS. No borra los archivos
    // de soportes/firma (eso lo hace el llamador vía IContingenciaArchivoService, antes de
    // llamar esto, ya que ahí sí conoce las rutas relativas).
    Task<bool> EliminarAsync(string cia, string radicado);
}
