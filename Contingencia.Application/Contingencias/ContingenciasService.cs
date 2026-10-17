using System.Data;
using System.Globalization;
using System.Text.Json;
using Contingencia.Infrastructure.Contingencias;

namespace Contingencia.Application.Contingencias;

public class ContingenciasService(IContingenciasRepository repository) : IContingenciasService
{
    public async Task<ResultadoRegistrarContingencia> RegistrarAsync(string cia, string usuario, SolicitudNuevaContingencia s)
    {
        if (string.IsNullOrWhiteSpace(s.Causal))
        {
            return new ResultadoRegistrarContingencia(false, "Debe ingresar la causal de la contingencia.", null);
        }

        if (string.IsNullOrWhiteSpace(s.IpsCodigo))
        {
            return new ResultadoRegistrarContingencia(false, "Debe seleccionar la IPS.", null);
        }

        if (string.IsNullOrWhiteSpace(s.ProfesionalDocumento))
        {
            return new ResultadoRegistrarContingencia(false, "Debe seleccionar el profesional tratante.", null);
        }

        if (string.IsNullOrWhiteSpace(s.PacienteTipoDoc) || string.IsNullOrWhiteSpace(s.PacienteNumDoc)
            || string.IsNullOrWhiteSpace(s.PacienteNombres) || string.IsNullOrWhiteSpace(s.PacienteApellidos))
        {
            return new ResultadoRegistrarContingencia(false, "Debe buscar y confirmar el paciente.", null);
        }

        if (s.PacienteFechaAtencion is null || string.IsNullOrWhiteSpace(s.PacienteHoraAtencion))
        {
            return new ResultadoRegistrarContingencia(false, "Debe ingresar la fecha y hora de atención.", null);
        }

        if (string.IsNullOrWhiteSpace(s.PacienteAmbitoAtencionCodigo))
        {
            return new ResultadoRegistrarContingencia(false, "Debe seleccionar el ámbito de atención.", null);
        }

        if (string.IsNullOrWhiteSpace(s.DiagnosticoCodigo))
        {
            return new ResultadoRegistrarContingencia(false, "Debe seleccionar el diagnóstico (CIE-10).", null);
        }

        if (s.Tecnologias.Count == 0)
        {
            return new ResultadoRegistrarContingencia(false, "Debe registrar al menos una tecnología.", null);
        }

        for (var i = 0; i < s.Tecnologias.Count; i++)
        {
            var t = s.Tecnologias[i];
            if (string.IsNullOrWhiteSpace(t.TipoTecnologia))
            {
                return new ResultadoRegistrarContingencia(false, $"Debe seleccionar el tipo de la tecnología {i + 1}.", null);
            }

            if (string.IsNullOrWhiteSpace(t.JustificacionTecnica))
            {
                return new ResultadoRegistrarContingencia(false, $"Debe ingresar la justificación técnica de la tecnología {i + 1}.", null);
            }
        }

        var radicado = await repository.InsertarAsync(
            cia, usuario, s.Causal, s.CausalNombre, s.CausalDescripcion,
            s.IpsCodigo, s.IpsNombre,
            s.ProfesionalDocumento, s.ProfesionalNombre, s.ProfesionalProfesion,
            s.PacienteTipoDoc, s.PacienteNumDoc, s.PacienteNombres, s.PacienteApellidos,
            s.PacienteSexo, s.PacienteFechaNacimiento, s.PacienteMunicipio, s.PacienteTelefono, s.PacienteRegimen,
            s.PacienteFechaAtencion, s.PacienteHoraAtencion, s.PacienteAmbitoAtencionCodigo, s.PacienteAmbitoAtencionNombre,
            s.DiagnosticoCodigo, s.DiagnosticoDescripcion,
            s.DiagnosticoRel1Codigo, s.DiagnosticoRel1Descripcion, s.DiagnosticoRel2Codigo, s.DiagnosticoRel2Descripcion,
            JsonSerializer.Serialize(s.Tecnologias));

        return new ResultadoRegistrarContingencia(true, null, radicado);
    }

    public async Task<ResultadoRegistrarContingencia> ActualizarArchivosAsync(
        string cia, string radicado, string? firmaRuta, string? soportesRutasJson)
    {
        var actualizo = await repository.ActualizarArchivosAsync(cia, radicado, firmaRuta, soportesRutasJson);
        return actualizo
            ? new ResultadoRegistrarContingencia(true, null, radicado)
            : new ResultadoRegistrarContingencia(false, "No se encontró la contingencia para actualizar los archivos.", radicado);
    }

    public async Task<ResultadoConsultaContingencias> ConsultarAsync(string cia, string? usuario, FiltroConsultaContingencias filtro)
    {
        DateTime? fechaInicio = DateTime.TryParse(filtro.FechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fi) ? fi : null;
        DateTime? fechaFin = DateTime.TryParse(filtro.FechaFin, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ff) ? ff : null;

        var dt = await repository.ConsultarAsync(
            cia, usuario, filtro.Radicado, filtro.Causal, filtro.IpsCodigo,
            filtro.ProfesionalDocumento, filtro.PacienteNumDoc, filtro.TipoTecnologia, fechaInicio, fechaFin);

        if (dt.Rows.Count == 0)
        {
            return new ResultadoConsultaContingencias(true, null, []);
        }

        var radicados = dt.AsEnumerable().Select(f => Texto(f, "RADICADO")).ToList();
        var dtTec = await repository.ConsultarTecnologiasAsync(cia, JsonSerializer.Serialize(radicados));
        var tecnologiasPorRadicado = dtTec.AsEnumerable()
            .GroupBy(f => Texto(f, "RADICADO"))
            .ToDictionary(g => g.Key, g => (IReadOnlyList<TecnologiaContingencia>)g.Select(MapearTecnologia).ToList());

        var filas = dt.AsEnumerable()
            .Select(f => Mapear(f, tecnologiasPorRadicado.GetValueOrDefault(Texto(f, "RADICADO"), [])))
            .ToList();
        return new ResultadoConsultaContingencias(true, null, filas);
    }

    private static TecnologiaContingencia MapearTecnologia(DataRow f) => new(
        Texto(f, "TIPOTECNOLOGIA"),
        TextoNulo(f, "TEC_NOMBRE"), TextoNulo(f, "TEC_CODIGO"), TextoNulo(f, "TEC_PRINCIPIOACTIVO"), TextoNulo(f, "TEC_FORMAFARMACEUTICA"), TextoNulo(f, "TEC_VIAADMINISTRACION"), TextoNulo(f, "TEC_DOSIS"),
        TextoNulo(f, "POS_TIPOPRESTACION"), TextoNulo(f, "POS_CANTIDADFORMULADA"), TextoNulo(f, "POS_CANTIDADFORMULADAUNIDAD"), TextoNulo(f, "POS_CADAFRECUENCIA"), TextoNulo(f, "POS_FRECUENCIA"),
        TextoNulo(f, "POS_DURACIONTRATAMIENTO"), TextoNulo(f, "POS_DURACIONUNIDAD"), TextoNulo(f, "POS_INDICACIONESPECIAL"),
        TextoNulo(f, "POS_CANTIDADTOTAL"), TextoNulo(f, "POS_INDICADORRECURRENCIA"), TextoNulo(f, "POS_INDICACIONESADICIONALES"),
        Texto(f, "JUSTIFICACION_TECNICA"));

    private static ContingenciaConsultada Mapear(DataRow f, IReadOnlyList<TecnologiaContingencia> tecnologias) => new(
        Convert.ToInt32(f["ID"]),
        Texto(f, "CIA"), Texto(f, "RADICADO"), Texto(f, "ESTADO"), Texto(f, "USUARIO"), Convert.ToDateTime(f["FECHACREACION"]),
        Texto(f, "CAUSAL"), TextoNulo(f, "CAUSAL_NOMBRE"), TextoNulo(f, "CAUSAL_DESCRIPCION"), TextoNulo(f, "IPS_CODIGO"), TextoNulo(f, "IPS_NOMBRE"),
        Texto(f, "PROFESIONAL_DOCUMENTO"), TextoNulo(f, "PROFESIONAL_NOMBRE"), TextoNulo(f, "PROFESIONAL_PROFESION"),
        Texto(f, "PACIENTE_TIPODOC"), Texto(f, "PACIENTE_NUMDOC"), Texto(f, "PACIENTE_NOMBRES"), Texto(f, "PACIENTE_APELLIDOS"),
        TextoNulo(f, "PACIENTE_SEXO"), FechaNula(f, "PACIENTE_FECHANACIMIENTO"), TextoNulo(f, "PACIENTE_MUNICIPIO"),
        TextoNulo(f, "PACIENTE_TELEFONO"), TextoNulo(f, "PACIENTE_REGIMEN"),
        FechaNula(f, "PACIENTE_FECHAATENCION"), TextoNulo(f, "PACIENTE_HORAATENCION"),
        TextoNulo(f, "PACIENTE_AMBITOATENCION_CODIGO"), TextoNulo(f, "PACIENTE_AMBITOATENCION_NOMBRE"),
        Texto(f, "DIAGNOSTICO_CODIGO"), TextoNulo(f, "DIAGNOSTICO_DESCRIPCION"),
        TextoNulo(f, "DIAGNOSTICO_REL1_CODIGO"), TextoNulo(f, "DIAGNOSTICO_REL1_DESCRIPCION"),
        TextoNulo(f, "DIAGNOSTICO_REL2_CODIGO"), TextoNulo(f, "DIAGNOSTICO_REL2_DESCRIPCION"),
        tecnologias,
        TextoNulo(f, "SOPORTES_RUTAS_JSON"), TextoNulo(f, "FIRMA_RUTA"), Convert.ToBoolean(f["FIRMAPENDIENTEFEA"]));

    private static string Texto(DataRow f, string columna) => Convert.ToString(f[columna]) ?? "";
    private static string? TextoNulo(DataRow f, string columna) => f[columna] is DBNull ? null : Convert.ToString(f[columna]);
    private static DateTime? FechaNula(DataRow f, string columna) => f[columna] is DBNull ? null : Convert.ToDateTime(f[columna]);
}
