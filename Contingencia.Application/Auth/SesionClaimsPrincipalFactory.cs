using System.Security.Claims;
using Contingencia.Domain.SesionPuente;

namespace Contingencia.Application.Auth;

// Construye el ClaimsPrincipal de esta app a partir de una fila de la tabla puente. Es el
// equivalente a las variables de Session que poblaba Login3.aspx.vb tras un login exitoso.
public static class SesionClaimsPrincipalFactory
{
    public const string EsquemaAutenticacion = "ContingenciaCookie";

    public static ClaimsPrincipal Crear(DatosSesionPuente datos, IReadOnlyList<string> modulos)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, datos.Nombre),
            new(ClaimTypes.Role, datos.Rol),
            new(ImperiumClaimTypes.Cia, datos.Cia),
            new(ImperiumClaimTypes.UsuApp, datos.UsuApp),
            new(ImperiumClaimTypes.IdEmp, datos.IdEmp),
            new(ImperiumClaimTypes.Logo, datos.Logo),
            new(ImperiumClaimTypes.LogoEmpresa, datos.LogoEmpresa),
            new(ImperiumClaimTypes.CodigoEps, datos.CodigoEps),
            new(ImperiumClaimTypes.ModActIni, datos.ModActIni),
            new(ImperiumClaimTypes.Log, datos.Log),
            new(ImperiumClaimTypes.RutaPdf, datos.RutaPdf),
            new(ImperiumClaimTypes.TextoCarta, datos.TextoCarta),
            new(ImperiumClaimTypes.TextoNotificacion, datos.TextoNotificacion),
            new(ImperiumClaimTypes.Firma, datos.Firma),
            new(ImperiumClaimTypes.NombreCliente, datos.NombreCliente),
            new(ImperiumClaimTypes.NitCliente, datos.NitCliente),
            new(ImperiumClaimTypes.IdRol, datos.IdRol),
            new(ImperiumClaimTypes.TokenMipres, datos.TokenMipres),
        };

        claims.AddRange(modulos.Select(modulo => new Claim(ImperiumClaimTypes.Modulo, modulo)));

        var identity = new ClaimsIdentity(claims, EsquemaAutenticacion, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
