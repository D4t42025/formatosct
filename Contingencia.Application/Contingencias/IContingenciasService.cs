namespace Contingencia.Application.Contingencias;

// Una tecnología de la contingencia (pasos 6 a 9 del wizard: tipo, detalle, posología y
// justificación). Una contingencia puede tener varias; el orden de la lista es el ORDEN en
// DQS_MIP_CONTINGENCIAS_TEC. Los nombres de las propiedades se serializan tal cual a JSON y
// deben coincidir con las rutas ('$.TipoTecnologia', ...) de sp_MIP_InsertaContingencia.
public record TecnologiaContingencia(
    string TipoTecnologia,
    string? TecNombre, string? TecCodigo, string? TecPrincipioActivo, string? TecFormaFarmaceutica, string? TecViaAdministracion, string? TecDosis,
    string? PosTipoPrestacion, string? PosCantidadFormulada, string? PosCantidadFormuladaUnidad, string? PosCadaFrecuencia, string? PosFrecuencia,
    string? PosDuracionTratamiento, string? PosDuracionUnidad, string? PosIndicacionEspecial,
    string? PosCantidadTotal, string? PosIndicadorRecurrencia, string? PosIndicacionesAdicionales,
    string JustificacionTecnica);

// Datos capturados por los 11 pasos del wizard "Nueva Contingencia MIPRES", tal como los
// arma NuevaContingencia.razor al confirmar el paso de Revisión. No incluye rutas de
// archivos (firma/soportes) porque esas se conocen DESPUÉS de tener el radicado — se
// completan con ActualizarArchivosAsync una vez guardados en disco.
public record SolicitudNuevaContingencia(
    string Causal, string? CausalNombre, string? CausalDescripcion,
    string? IpsCodigo, string? IpsNombre,
    string ProfesionalDocumento, string? ProfesionalNombre, string? ProfesionalProfesion,
    string PacienteTipoDoc, string PacienteNumDoc, string PacienteNombres, string PacienteApellidos,
    string? PacienteSexo, DateTime? PacienteFechaNacimiento, string? PacienteMunicipio,
    string? PacienteTelefono, string? PacienteRegimen,
    DateTime? PacienteFechaAtencion, string? PacienteHoraAtencion,
    string? PacienteAmbitoAtencionCodigo, string? PacienteAmbitoAtencionNombre,
    string DiagnosticoCodigo, string? DiagnosticoDescripcion,
    string? DiagnosticoRel1Codigo, string? DiagnosticoRel1Descripcion,
    string? DiagnosticoRel2Codigo, string? DiagnosticoRel2Descripcion,
    IReadOnlyList<TecnologiaContingencia> Tecnologias);

public record ResultadoRegistrarContingencia(bool Exito, string? Mensaje, string? Radicado);

public record ContingenciaConsultada(
    int Id, string Cia, string Radicado, string Estado, string Usuario, DateTime FechaCreacion,
    string Causal, string? CausalNombre, string? CausalDescripcion, string? IpsCodigo, string? IpsNombre,
    string ProfesionalDocumento, string? ProfesionalNombre, string? ProfesionalProfesion,
    string PacienteTipoDoc, string PacienteNumDoc, string PacienteNombres, string PacienteApellidos,
    string? PacienteSexo, DateTime? PacienteFechaNacimiento, string? PacienteMunicipio,
    string? PacienteTelefono, string? PacienteRegimen,
    DateTime? PacienteFechaAtencion, string? PacienteHoraAtencion,
    string? PacienteAmbitoAtencionCodigo, string? PacienteAmbitoAtencionNombre,
    string DiagnosticoCodigo, string? DiagnosticoDescripcion,
    string? DiagnosticoRel1Codigo, string? DiagnosticoRel1Descripcion,
    string? DiagnosticoRel2Codigo, string? DiagnosticoRel2Descripcion,
    IReadOnlyList<TecnologiaContingencia> Tecnologias,
    string? SoportesRutasJson, string? FirmaRuta, bool FirmaPendienteFea)
{
    // Tipos de tecnología distintos, en orden de aparición — para la columna de la grilla.
    public string TiposTecnologia => string.Join(", ", Tecnologias.Select(t => t.TipoTecnologia).Distinct());
}

public record ResultadoConsultaContingencias(bool Exito, string? Mensaje, IReadOnlyList<ContingenciaConsultada> Contingencias);

public record FiltroConsultaContingencias(
    string? Radicado = null, string? Causal = null, string? IpsCodigo = null,
    string? ProfesionalDocumento = null, string? PacienteNumDoc = null, string? TipoTecnologia = null,
    string? FechaInicio = null, string? FechaFin = null);

public interface IContingenciasService
{
    // Valida en orden (causal → IPS → profesional → paciente → diagnóstico → tecnologías),
    // mismo estilo que FormatosService.ValidarYRegistrarAsync. Exige al menos una tecnología y
    // que cada una tenga tipo y justificación. No recibe firma/soportes: esos se guardan
    // después con ActualizarArchivosAsync, ya con el radicado que devuelve este método.
    Task<ResultadoRegistrarContingencia> RegistrarAsync(string cia, string usuario, SolicitudNuevaContingencia solicitud);

    Task<ResultadoRegistrarContingencia> ActualizarArchivosAsync(
        string cia, string radicado, string? firmaRuta, string? soportesRutasJson);

    Task<ResultadoConsultaContingencias> ConsultarAsync(string cia, string? usuario, FiltroConsultaContingencias filtro);

    // Borra los registros de DQS_MIP_CONTINGENCIAS + DQS_MIP_CONTINGENCIAS_TEC. Restringido
    // en el llamador (Razor) a la cuenta "Imperium" vía
    // ClaimsPrincipalExtensions.PuedeEliminarComoImperium — este método no valida permisos.
    // No borra los archivos de soportes/firma (lo hace el llamador antes de invocar esto).
    Task<ResultadoRegistrarContingencia> EliminarAsync(string cia, string radicado);
}
