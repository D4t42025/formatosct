namespace Contingencia.Infrastructure.Contingencias.FormularioPdf;

// Datos ya resueltos (nombres partidos, códigos normalizados, NIT consultado) para pintar el
// formulario oficial MIPRES NoPBS. Lo que no se tiene queda null/vacío y el formulario lo deja
// en blanco para diligenciarlo a mano.
public record TecnologiaFormulario(
    string TipoTecnologia,
    string? Nombre, string? Codigo, string? PrincipioActivo,
    string? FormaFarmaceutica, string? ViaAdministracion,
    string? ConcentracionNumero, string? ConcentracionUnidad,
    string? DosisNumero, string? DosisUnidad,
    string? CadaNumero, string? CadaUnidad,
    string? IndicacionEspecial,
    string? DuracionNumero, string? DuracionPeriodo,
    string? CantidadTotal,
    string? IndicacionesAdicionales,
    string Justificacion);

public record DatosFormularioContingencia(
    DateTime FechaDiligenciamiento,
    // I. Motivo: "DIFICULTAD_TECNICA" | "SIN_CONECTIVIDAD" | "SIN_SERVICIO_ELECTRICO" | null (ninguna casilla)
    string? MotivoCasilla, string? Aclaracion,
    // II. IPS
    string? IpsRazonSocial, string? IpsNit, string? IpsCodigoHabilitacion,
    // III. Profesional
    string? ProfesionalTipoDoc, string? ProfesionalDocumento,
    string? ProfesionalPrimerApellido, string? ProfesionalSegundoApellido,
    string? ProfesionalPrimerNombre, string? ProfesionalSegundoNombre,
    // IV. Datos iniciales
    string? PacienteTipoDoc, string? PacienteDocumento,
    string? PacientePrimerApellido, string? PacienteSegundoApellido,
    string? PacientePrimerNombre, string? PacienteSegundoNombre,
    string? AmbitoCodigo,
    string? DiagnosticoPrincipal, string? DiagnosticoRelacionado1, string? DiagnosticoRelacionado2,
    IReadOnlyList<TecnologiaFormulario> Tecnologias,
    byte[]? Firma);
