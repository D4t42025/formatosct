using System.Data;
using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.Formatos;

// Equivalente a las llamadas de JuntasFormatoCntC.aspx.vb: objbll.insertafotmatocontigencia
// (sp_MIP_InsertaFormatoCnt) y el WebMethod "Ciudad" (autocompletado del campo "Código de
// Habilitación - Prestador", que en el legado concatenaba el texto tecleado directamente en
// el SQL — inyección real. Aquí va parametrizado, mismo criterio ya usado en
// LiquidacionAutocompleteRepository de Recobros.
public class FormatosRepository(IStoredProcedureExecutor executor) : IFormatosRepository
{
    public async Task<string> InsertarAsync(string cia, string tipoId, string numId, string nit, string obj,
        string extension, string email, string fechaFormato, string usuario)
    {
        var dt = await executor.RecuperarDataTableAsync("sp_MIP_InsertaFormatoCnt",
            cia, tipoId, numId, nit, obj, extension, email, fechaFormato, usuario);
        return Convert.ToString(dt.Rows[0][0]) ?? "";
    }

    public Task<DataTable> BuscarPrestadorAsync(string prefijo, int cantidad, string? nit = null) =>
        executor.RecuperarDataTablePorSqlAsync(
            """
            SELECT DISTINCT TOP (@Cantidad) CAST([Codigo] AS VARCHAR(100)) AS Codigo, [Nombre]
            FROM DQS_MIP_COD_HAB
            WHERE ([Nombre] LIKE @Prefijo + '%' OR [Codigo] LIKE @Prefijo + '%')
              AND (@Nit IS NULL OR [Extra_II_NroIDPrestador] = @Nit)
            ORDER BY [Nombre] ASC
            """,
            ("@Cantidad", cantidad), ("@Prefijo", prefijo), ("@Nit", nit));

    public Task<DataTable> ObtenerDetalleAsync(string codigo, string? nit = null) =>
        executor.RecuperarDataTablePorSqlAsync(
            """
            SELECT TOP (1) CAST([Codigo] AS VARCHAR(100)) AS Codigo, [Nombre],
                   [Extra_II_NroIDPrestador] AS Nit,
                   [Extra_VI_NombreDptoSede] AS Departamento,
                   [Extra_V_NombreMpioSede] AS Municipio,
                   [Extra_VIII] AS Clase,
                   [Habilitado]
            FROM DQS_MIP_COD_HAB
            WHERE [Codigo] = @Codigo AND (@Nit IS NULL OR [Extra_II_NroIDPrestador] = @Nit)
            """,
            ("@Codigo", codigo), ("@Nit", nit));

    // sp_MIP_ConsultaFormatosCNT solo tiene 7 parámetros reales (confirmado contra la base
    // de pruebas) aunque JuntasFormatoCntCO.aspx.vb arma un objeto con 8 campos — "fecha fin"
    // nunca se envía al SP. Los campos vacíos se sustituyen por "%" antes de llamar, igual
    // que btnconsultar_ServerClick, excepto @USUARIO que siempre es el usuario de sesión.
    public Task<DataTable> ConsultarAsync(string cia, string tipo, string numeroDocumento, string prestador,
        string radicado, string fecha, string usuario) =>
        executor.RecuperarDataTableAsync("sp_MIP_ConsultaFormatosCNT",
            cia, tipo, numeroDocumento, prestador, radicado, fecha, usuario);

    public Task ActualizarGestionAsync(string cia, string mipres, string obj, string estado, string usuario, string radicado) =>
        executor.EjecutarProcedimientoAsync("sp_MIP_ActualizaFormatoCnt",
            cia, mipres, obj, estado, usuario, radicado);

    public async Task<string?> ConsultarEmailAsync(string cia, string radicado)
    {
        var dt = await executor.RecuperarDataTablePorSqlAsync(
            "SELECT EMAIL FROM DQS_MIP_FORMATOS WHERE CIA = @Cia AND RADICADO = @Radicado",
            ("@Cia", cia), ("@Radicado", radicado));
        return dt.Rows.Count > 0 ? Convert.ToString(dt.Rows[0]["EMAIL"]) : null;
    }

    public async Task<bool> EliminarAsync(string cia, string radicado)
    {
        var dt = await executor.RecuperarDataTableAsync("sp_MIP_EliminaFormatoCnt", cia, radicado);
        return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["RESULTADO"]) == 1;
    }
}
