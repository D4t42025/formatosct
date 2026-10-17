using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Contingencia.Infrastructure.Afiliados;

// WS de afiliados de EPS Sanitas — confirmado con una captura real de Postman del usuario
// (2026-09-30): POST con body JSON {TIPO_IDENTIFICACION, NUMERO_IDENTIFICACION} y header
// Authorization: Bearer <token> (JWT, guardado en dbo.DQS_MIP_AFILIADOS_TOKEN, ver
// AfiliadoTokenRepository). Nunca lanza excepción hacia el llamador — cualquier falla de red
// o del servicio se envuelve en ResultadoConsultaAfiliadoApi.Exito=false, mismo criterio que
// MipresPrescripcionValidator para el WS de SISPRO.
public class AfiliadoApiClient(IAfiliadoTokenRepository tokenRepository, HttpClient httpClient) : IAfiliadoApiClient
{
    // Por defecto el de EPS Sanitas, el único que existe hoy; otra CIA con web service propio lo indica en AFILIADOS_WS_URL.
    private const string UrlPorDefecto = "https://imperiumgitsanitas.net:51044/tutelas/tutelasmipres";

    public async Task<ResultadoConsultaAfiliadoApi> ConsultarAsync(string tipoDocumento, string numeroDocumento, string? url = null)
    {
        var token = await tokenRepository.ObtenerVigenteAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ResultadoConsultaAfiliadoApi(false, "No hay un token configurado para el servicio de afiliados.", null);
        }

        try
        {
            using var mensaje = new HttpRequestMessage(HttpMethod.Post, string.IsNullOrWhiteSpace(url) ? UrlPorDefecto : url)
            {
                Content = JsonContent.Create(new SolicitudConsultaAfiliadoApi
                {
                    TipoIdentificacion = tipoDocumento,
                    NumeroIdentificacion = numeroDocumento,
                }),
            };
            mensaje.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var respuesta = await httpClient.SendAsync(mensaje);
            if (!respuesta.IsSuccessStatusCode)
            {
                return new ResultadoConsultaAfiliadoApi(
                    false, $"El servicio de afiliados respondió con error ({(int)respuesta.StatusCode}).", null);
            }

            var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaConsultaAfiliadoApi>();
            if (cuerpo is not { Success: true, Respuesta: not null })
            {
                return new ResultadoConsultaAfiliadoApi(false, "No se encontró un afiliado con ese documento.", null);
            }

            return new ResultadoConsultaAfiliadoApi(true, null, cuerpo.Respuesta);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new ResultadoConsultaAfiliadoApi(false, "No fue posible conectar con el servicio de afiliados. Intente de nuevo.", null);
        }
    }
}
