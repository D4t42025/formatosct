namespace Contingencia.Infrastructure.Afiliados;

public record ResultadoConsultaAfiliadoApi(bool Exito, string? Mensaje, RespuestaAfiliadoApi? Datos);

public interface IAfiliadoApiClient
{
    // url: dirección del web service de la CIA (parámetro AFILIADOS_WS_URL); null = la de EPS Sanitas.
    Task<ResultadoConsultaAfiliadoApi> ConsultarAsync(string tipoDocumento, string numeroDocumento, string? url = null);
}
