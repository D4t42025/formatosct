using System.Text.RegularExpressions;
using Contingencia.Application.Formatos;
using Contingencia.Application.Profesionales;
using Contingencia.Infrastructure.Contingencias.FormularioPdf;

namespace Contingencia.Application.Contingencias;

public interface IContingenciaFormularioService
{
    // Formulario oficial MIPRES NoPBS ("Formulario para contingencia") de una contingencia ya
    // registrada, con los datos que el wizard captura y lo demás en blanco.
    Task<byte[]> GenerarAsync(string cia, ContingenciaConsultada contingencia, byte[]? firma);
}

public partial class ContingenciaFormularioService(
    IFormatosService formatosService,
    IProfesionalesService profesionalesService,
    IFormularioContingenciaPdfBuilder builder) : IContingenciaFormularioService
{
    public async Task<byte[]> GenerarAsync(string cia, ContingenciaConsultada c, byte[]? firma)
    {
        // El NIT no se guarda en la contingencia (solo el código y el nombre de la IPS): se
        // vuelve a leer de DQS_MIP_COD_HAB por el código de habilitación.
        var ips = string.IsNullOrWhiteSpace(c.IpsCodigo) ? null : await formatosService.ObtenerDetallePrestadorAsync(c.IpsCodigo);

        // Tipo de documento y nombres/apellidos por separado vienen de la ficha del profesional
        // (la contingencia solo guarda el nombre completo ya armado).
        var profesionales = await profesionalesService.ConsultarAsync(cia, c.ProfesionalDocumento, null, null, null);
        var profesional = profesionales.FirstOrDefault(p => p.NoIdentificacion == c.ProfesionalDocumento);
        var (apellido1Prof, apellido2Prof) = PartirPrimeraPalabra(profesional?.Apellidos);
        var (nombre1Prof, nombre2Prof) = PartirPrimeraPalabra(profesional?.Nombres);

        var (apellido1Pac, apellido2Pac) = PartirPrimeraPalabra(c.PacienteApellidos);
        var (nombre1Pac, nombre2Pac) = PartirPrimeraPalabra(c.PacienteNombres);

        var (motivoCasilla, aclaracion) = Motivo(c);

        var datos = new DatosFormularioContingencia(
            c.FechaCreacion,
            motivoCasilla, aclaracion,
            LimpiarTexto(ips?.Nombre ?? c.IpsNombre), ips?.Nit, c.IpsCodigo,
            profesional?.TipoDoc, c.ProfesionalDocumento,
            apellido1Prof, apellido2Prof, nombre1Prof, nombre2Prof,
            c.PacienteTipoDoc, c.PacienteNumDoc,
            apellido1Pac, apellido2Pac, nombre1Pac, nombre2Pac,
            c.PacienteAmbitoAtencionCodigo,
            Diagnostico(c.DiagnosticoCodigo, c.DiagnosticoDescripcion),
            Diagnostico(c.DiagnosticoRel1Codigo, c.DiagnosticoRel1Descripcion),
            Diagnostico(c.DiagnosticoRel2Codigo, c.DiagnosticoRel2Descripcion),
            c.Tecnologias.Select(Tecnologia).ToList(),
            firma);

        return builder.Generar(datos);
    }

    // Solo tres de las causales del wizard tienen casilla propia en la sección I del formulario;
    // para las demás (urgencia, inconsistencias, causales creadas por el usuario) no se marca
    // ninguna y la causal viaja en la aclaración.
    private static (string? casilla, string? aclaracion) Motivo(ContingenciaConsultada c)
    {
        var casilla = c.Causal switch
        {
            "DIFICULTAD_TECNICA" or "SIN_CONECTIVIDAD" or "SIN_SERVICIO_ELECTRICO" => c.Causal,
            _ => null,
        };

        var aclaracion = casilla is null
            ? string.Join(": ", new[] { c.CausalNombre ?? c.Causal, c.CausalDescripcion }.Where(t => !string.IsNullOrWhiteSpace(t)))
            : c.CausalDescripcion;
        return (casilla, aclaracion);
    }

    private static TecnologiaFormulario Tecnologia(TecnologiaContingencia t)
    {
        var (concentracionNumero, concentracionUnidad) = PartirConcentracion(t.TecDosis);

        // En Medicamento "cantidad total" lleva solo el número (la unidad farmacéutica no se
        // captura y queda en blanco); en los demás tipos es la "cantidad formulada".
        return new TecnologiaFormulario(
            t.TipoTecnologia,
            t.TecNombre, t.TecCodigo, t.TecPrincipioActivo,
            t.TecFormaFarmaceutica, t.TecViaAdministracion,
            concentracionNumero, concentracionUnidad,
            t.PosCantidadFormulada, t.PosCantidadFormuladaUnidad,
            t.PosCadaFrecuencia, t.PosFrecuencia,
            t.PosIndicacionEspecial,
            t.PosDuracionTratamiento, t.PosDuracionUnidad,
            t.PosCantidadTotal,
            t.PosIndicacionesAdicionales,
            t.JustificacionTecnica);
    }

    // Algunos nombres de DQS_MIP_COD_HAB vienen con comillas y espacios sobrantes (ej. '" NATURAL BIOLOGY IPS SAS"').
    private static string? LimpiarTexto(string? texto) => texto?.Trim().Trim('"').Trim();

    private static string? Diagnostico(string? codigo, string? descripcion) =>
        string.IsNullOrWhiteSpace(codigo) ? null : string.IsNullOrWhiteSpace(descripcion) ? codigo : $"{codigo} - {descripcion}";

    // "Pinzon Mayorga" -> ("Pinzon", "Mayorga"); "De La Cruz" -> ("De", "La Cruz"). El formulario
    // tiene casillas para primer y segundo apellido/nombre; el wizard solo guarda el texto
    // completo, así que se parte por la primera palabra.
    private static (string? primero, string? segundo) PartirPrimeraPalabra(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return (null, null);
        var partes = texto.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (partes[0], partes.Length > 1 ? partes[1] : null);
    }

    // La concentración se captura como un solo texto ("500 mg"); el formulario pide número y
    // unidad de medida por separado. Si el texto no empieza por un número, el valor completo
    // va en la unidad y el número queda en blanco.
    private static (string? numero, string? unidad) PartirConcentracion(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return (null, null);
        var m = ConcentracionRegex().Match(texto);
        return m.Success ? (m.Groups[1].Value, string.IsNullOrWhiteSpace(m.Groups[2].Value) ? null : m.Groups[2].Value.Trim()) : (null, texto.Trim());
    }

    [GeneratedRegex(@"^\s*(\d+(?:[.,]\d+)?)\s*(.*)$")]
    private static partial Regex ConcentracionRegex();
}
