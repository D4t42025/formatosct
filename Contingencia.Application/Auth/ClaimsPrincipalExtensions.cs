using System.Security.Claims;

namespace Contingencia.Application.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string Cia(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.Cia)?.Value ?? "";
    public static string Rol(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value ?? "";
    public static string Nombre(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Name)?.Value ?? "";
    public static string UsuApp(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.UsuApp)?.Value ?? "";
    public static string IdEmp(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.IdEmp)?.Value ?? "";
    public static string Logo(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.Logo)?.Value ?? "";
    public static string LogoEmpresa(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.LogoEmpresa)?.Value ?? "";
    public static string Log(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.Log)?.Value ?? "";
    public static string RutaPdf(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.RutaPdf)?.Value ?? "";
    public static string TextoCarta(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.TextoCarta)?.Value ?? "";
    public static string TextoNotificacion(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.TextoNotificacion)?.Value ?? "";
    public static string Firma(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.Firma)?.Value ?? "";
    public static string NombreCliente(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.NombreCliente)?.Value ?? "";
    public static string NitCliente(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.NitCliente)?.Value ?? "";
    public static string IdRol(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.IdRol)?.Value ?? "";
    public static string TokenMipres(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.TokenMipres)?.Value ?? "";

    // Roles que en el legado ven el ícono de lápiz "Gestion" en JuntasFormatoCntCO.aspx y
    // pueden usar JuntasFormatoCntCO2.aspx (migrado a un modal) — confirmado por el usuario
    // (2026-09-02): 27, 29 y 30. El propio SP (sp_MIP_ConsultaFormatosCNT) no distingue más
    // que "601" vs "cualquier otro rol", así que esta lista es una decisión de la capa nueva,
    // no algo que exista como tal en la base.
    public static bool PuedeGestionarFormatos(this ClaimsPrincipal user) =>
        user.IdRol() is "27" or "29" or "30";

    // Restricción deliberadamente más estricta que PuedeGestionarFormatos: el borrado es
    // destructivo (BD + archivo/blob), así que solo lo puede usar la cuenta interna de
    // validación "Imperium" de la CIA 890330031 (ver MIGRACION-AZURE-JUNTAS.md sección 29),
    // no cualquier usuario que tenga rol 27/29/30. Decisión explícita del usuario
    // (2026-10-18, confirmada por ask_user): comparación literal de usuario + CIA, no de rol.
    // Se usa tanto en "Consultar formato" como en "Consultar nueva contingencia".
    public static bool PuedeEliminarComoImperium(this ClaimsPrincipal user) =>
        user.Cia() == "890330031" && string.Equals(user.UsuApp(), "Imperium", StringComparison.OrdinalIgnoreCase);
    public static bool ModuloInicioActivo(this ClaimsPrincipal user) => user.FindFirst(ImperiumClaimTypes.ModActIni)?.Value == "1";

    public static bool TieneModulo(this ClaimsPrincipal user, string texto) =>
        user.FindAll(ImperiumClaimTypes.Modulo).Any(c => c.Value.Contains(texto, StringComparison.Ordinal));

    public static bool TieneModuloExacto(this ClaimsPrincipal user, string nombre) =>
        user.FindAll(ImperiumClaimTypes.Modulo).Any(c => c.Value == nombre);
}
