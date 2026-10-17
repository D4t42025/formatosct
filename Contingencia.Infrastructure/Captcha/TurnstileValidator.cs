using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Contingencia.Infrastructure.Captcha;

// Login3.aspx (legado) traía el control <cc1:GoogleReCaptcha> comentado/deshabilitado —
// no había captcha activo que migrar. El usuario pidió agregar uno "más moderno" en esta
// versión nueva: se eligió Cloudflare Turnstile en vez de reCAPTCHA v2 (el que sí usa
// Recobros) porque no depende de una cuenta de Google, es gratuito sin límite de uso, y no
// exige que el usuario resuelva un desafío visual en la mayoría de los casos (challenge
// "administrado": la mayoría de visitantes pasa sin hacer nada, solo un ícono de carga).
//
// Site key / secret key por defecto: las claves de PRUEBA públicas y documentadas por
// Cloudflare (https://developers.cloudflare.com/turnstile/troubleshooting/testing/), que
// siempre muestran el widget visible y siempre validan con éxito — sirven para desarrollar
// sin depender de credenciales reales. ANTES de producción hay que reemplazarlas por un
// site key/secret real (gratis, se crea en https://dash.cloudflare.com/?to=/:account/turnstile).
public class TurnstileValidator(HttpClient httpClient, IConfiguration configuration, IHostEnvironment environment) : ITurnstileValidator
{
    public async Task<bool> EsValidoAsync(string respuestaCliente)
    {
        if (environment.IsDevelopment() && configuration.GetValue<bool>("Turnstile:Skip"))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(respuestaCliente))
        {
            return false;
        }

        var secretKey = configuration["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Falta configurar 'Turnstile:SecretKey'.");
        }

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = secretKey,
            ["response"] = respuestaCliente,
        });

        var respuesta = await httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", contenido);
        respuesta.EnsureSuccessStatusCode();

        using var stream = await respuesta.Content.ReadAsStreamAsync();
        using var documento = await JsonDocument.ParseAsync(stream);
        return documento.RootElement.TryGetProperty("success", out var success) && success.GetBoolean();
    }
}
