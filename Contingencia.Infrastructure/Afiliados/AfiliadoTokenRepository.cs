using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.Afiliados;

// Token del WS de afiliados de EPS Sanitas (dbo.DQS_MIP_AFILIADOS_TOKEN) — guardado en base
// de datos, no en appsettings.json, porque es un JWT con fecha de expiración que hay que
// poder rotar sin redeploy. Mismo criterio que DQS_MIP_TOKEN para el WS de SISPRO/MIPRES.
public class AfiliadoTokenRepository(IStoredProcedureExecutor executor) : IAfiliadoTokenRepository
{
    public async Task<string?> ObtenerVigenteAsync()
    {
        var dt = await executor.RecuperarDataTableAsync("sp_MIP_ConsultaTokenAfiliados");
        return dt.Rows.Count > 0 ? Convert.ToString(dt.Rows[0]["TOKEN"]) : null;
    }
}
