using System.Data;
using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.CiaParametros;

// Parámetros por CIA (multi-CIA) de dbo.DQS_MIP_CIA_PARAMETROS, sin precedente legado — ver
// Sql/011_MipCiaParametros.sql.
public class CiaParametrosRepository(IStoredProcedureExecutor executor) : ICiaParametrosRepository
{
    public Task<DataTable> ConsultarAsync(string cia) =>
        executor.RecuperarDataTableAsync("sp_MIP_ConsultaCiaParametros", cia);
}
