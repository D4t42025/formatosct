using System.Data;
using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.Parametricas;

// Catálogo paramétrico genérico del wizard "Nueva Contingencia" (tabla
// dbo.DQS_MIP_CONT_PARAMETRICAS, sin precedente legado). Trae TODO el catálogo en una sola
// llamada — mismo criterio ya usado para DQS_MIP_CRUZAR (ver JuntasService.CargarCruzarAsync):
// se agrupa por GRUPO en la capa de aplicación, no se pide un grupo a la vez.
public class ParametricasRepository(IStoredProcedureExecutor executor) : IParametricasRepository
{
    public Task<DataTable> ConsultarAsync() =>
        executor.RecuperarDataTableAsync("sp_MIP_ConsultaParametricas", (object?)null);

    public async Task<(bool Exito, string Mensaje)> InsertarAsync(string grupo, string codigo, string valor)
    {
        var dt = await executor.RecuperarDataTableAsync("sp_MIP_InsertaParametrica", grupo, codigo, valor);
        var fila = dt.Rows[0];
        return (Convert.ToInt32(fila["RESULTADO"]) == 1, Convert.ToString(fila["MENSAJE"]) ?? "");
    }
}
