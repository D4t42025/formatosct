using System.Data;
using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.Filtros;

// Orden de parámetros confirmado contra sys.parameters de sp_DQS_ConsultaFiltros
// (STRCIA, INTVALIDA, STRUSUARIO, STRTIPO, STRPAQUETE, STRFACTURA, STRSERVICIO,
// STROPCION, STRESTADO, STREGIMEN, STRSUCURSAL, STRTIPOSERVICIO, STRENTE) — mismo criterio
// que Imperium.Infrastructure.Filtros.FiltrosRepository (Recobros).
public class FiltrosRepository(IStoredProcedureExecutor executor) : IFiltrosRepository
{
    public Task<DataTable> ConsultarAsync(
        string cia,
        int valida,
        string usuario = "",
        string tipo = "",
        string paquete = "",
        string factura = "",
        string servicio = "",
        string opcion = "",
        string estado = "",
        string regimen = "",
        string sucursal = "",
        string tiposervicio = "",
        string ente = "") =>
        executor.RecuperarDataTableAsync(
            "sp_DQS_ConsultaFiltros",
            cia, valida, usuario, tipo, paquete, factura, servicio, opcion, estado, regimen, sucursal, tiposervicio, ente);
}
