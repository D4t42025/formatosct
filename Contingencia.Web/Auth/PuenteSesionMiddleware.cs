using Contingencia.Application.Auth;
using Contingencia.Infrastructure.SesionPuente;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Contingencia.Web.Auth;

// Si el usuario llega desde una página .aspx todavía no migrada (con la cookie
// "ImperiumBridge" — mismo nombre que usa Recobros, pero cada app la borra al consumirla,
// así que no hay conflicto real aunque ambas convivan en el mismo navegador/dominio),
// consume el token de la tabla puente UNA sola vez y arma la cookie de autenticación de
// esta app.
public class PuenteSesionMiddleware(RequestDelegate next)
{
    private const string CookiePuente = "ImperiumBridge";

    public async Task InvokeAsync(HttpContext context, ISesionPuenteRepository sesionPuenteRepository)
    {
        if (context.Request.Cookies.TryGetValue(CookiePuente, out var valorCookie) &&
            Guid.TryParse(valorCookie, out var token))
        {
            var consumido = await sesionPuenteRepository.ConsumirAsync(token);
            context.Response.Cookies.Delete(CookiePuente);

            if (consumido is not null)
            {
                var principal = SesionClaimsPrincipalFactory.Crear(consumido.Value.Datos, consumido.Value.Modulos);
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                context.Response.Redirect(context.Request.Path + context.Request.QueryString);
                return;
            }
        }

        await next(context);
    }
}
