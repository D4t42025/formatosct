using System.Data;
using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.Contingencias;

// Wizard "Nueva Contingencia MIPRES" — tablas y SPs nuevos, sin precedente legado (ver
// Contingencia.Infrastructure/Sql/002_... y 010_MipContingenciasMultiplesTecnologias.sql).
// Mismo patrón que FormatosRepository (executor genérico, parámetros posicionales).
public class ContingenciasRepository(IStoredProcedureExecutor executor) : IContingenciasRepository
{
    public async Task<string> InsertarAsync(
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
        string tecnologiasJson)
    {
        var dt = await executor.RecuperarDataTableAsync("sp_MIP_InsertaContingencia",
            cia, usuario, causal, causalNombre, causalDescripcion,
            ipsCodigo, ipsNombre,
            profesionalDocumento, profesionalNombre, profesionalProfesion,
            pacienteTipoDoc, pacienteNumDoc, pacienteNombres, pacienteApellidos,
            pacienteSexo, pacienteFechaNacimiento, pacienteMunicipio, pacienteTelefono, pacienteRegimen,
            pacienteFechaAtencion, pacienteHoraAtencion, pacienteAmbitoAtencionCodigo, pacienteAmbitoAtencionNombre,
            diagnosticoCodigo, diagnosticoDescripcion,
            diagnosticoRel1Codigo, diagnosticoRel1Descripcion, diagnosticoRel2Codigo, diagnosticoRel2Descripcion,
            tecnologiasJson);

        return Convert.ToString(dt.Rows[0][0]) ?? "";
    }

    public async Task<bool> ActualizarArchivosAsync(string cia, string radicado, string? firmaRuta, string? soportesRutasJson)
    {
        var dt = await executor.RecuperarDataTableAsync("sp_MIP_ActualizaContingenciaArchivos",
            cia, radicado, firmaRuta, soportesRutasJson);
        return Convert.ToInt32(dt.Rows[0][0]) > 0;
    }

    public Task<DataTable> ConsultarAsync(
        string cia, string? usuario, string? radicado, string? causal, string? ipsCodigo,
        string? profesionalDocumento, string? pacienteNumDoc, string? tipoTecnologia,
        DateTime? fechaInicio, DateTime? fechaFin) =>
        executor.RecuperarDataTableAsync("sp_MIP_ConsultaContingencias",
            cia, usuario, radicado, causal, ipsCodigo,
            profesionalDocumento, pacienteNumDoc, tipoTecnologia, fechaInicio, fechaFin);

    public Task<DataTable> ConsultarTecnologiasAsync(string cia, string radicadosJson) =>
        executor.RecuperarDataTableAsync("sp_MIP_ConsultaContingenciaTecnologias", cia, radicadosJson);
}
