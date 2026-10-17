namespace Contingencia.Application.CiaParametros;

public enum OrigenAfiliados
{
    // Se consulta el web service de afiliados (hoy solo existe el de EPS Sanitas).
    WebService,
    // El usuario digita los datos del paciente en un formulario.
    Manual,
}

// Parámetros de una CIA que cambian el comportamiento del wizard "Nueva Contingencia".
// NitPrestadores: si la CIA es una IPS, NIT al que se restringe la lista de prestadores del paso 2
// del wizard (sus sedes); null = sin restricción (EPS).
// CodigosCatalogo: valor de DQS_MIP_CODIGOS.CIA del que se leen los códigos MIPRES del paso 7 (por defecto "NEPS", el único catálogo
// cargado hoy). UrlAfiliados: dirección del web service de afiliados de la CIA (solo modo WS); null = la de Sanitas por defecto.
public record ParametrosCia(
    OrigenAfiliados OrigenAfiliados, string? EntidadAfiliados, string? NitPrestadores = null,
    string CodigosCatalogo = ParametrosCia.CatalogoPorDefecto, string? UrlAfiliados = null)
{
    public const string CatalogoPorDefecto = "NEPS";
}

public interface ICiaParametrosService
{
    // Una CIA sin fila AFILIADOS_ORIGEN usa el formulario manual: nunca debe llamar al web
    // service de afiliados de otra entidad.
    Task<ParametrosCia> ObtenerAsync(string cia);
}
