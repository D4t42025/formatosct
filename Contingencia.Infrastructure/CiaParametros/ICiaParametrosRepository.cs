using System.Data;

namespace Contingencia.Infrastructure.CiaParametros;

public interface ICiaParametrosRepository
{
    // Filas CLAVE/VALOR activas de la CIA (tabla dbo.DQS_MIP_CIA_PARAMETROS).
    Task<DataTable> ConsultarAsync(string cia);
}
