using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Contingencia.Infrastructure.Codigos;

// Tabla DQS_MIP_CODIGOS: catálogo de códigos MIPRES (206.557 filas confirmadas en
// DQUALITYREC) para Procedimiento/Dispositivo médico/Producto Nutricional/Servicio
// complementario/Medicamento (columna Tipo), con el filtro real por TipoServicio
// (P/D/N/S/M). Tiene columna CIA (multi-tenant), pero HOY solo tiene filas cargadas para
// CIA='NEPS' — a pedido explícito del usuario la búsqueda usa ese catálogo; ya no está fijo en el código: cada CIA lo
// elige con el parámetro CODIGOS_CATALOGO de DQS_MIP_CIA_PARAMETROS (por defecto 'NEPS'; ver BASE_DE_DATOS.md). Sin SP existente
// para esto (confirmado buscando en todos los `sys.sql_modules` que referencian la tabla:
// son informes pesados de auditoría, no un lookup) — SQL directo, mismo patrón que
// Cie10Repository.
public class CodigosRepository(IConfiguration configuration) : ICodigosRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DQUALITY")
        ?? throw new InvalidOperationException("Falta configurar la cadena de conexión 'DQUALITY'.");

    public async Task<IReadOnlyList<CodigoFila>> BuscarAsync(string ciaCatalogo, string tipoServicio, string prefijo, int cantidad)
    {
        const string sql = """
            SELECT DISTINCT TOP (@Cantidad)
                   Codigo, CAST([Descripción] AS VARCHAR(1000)) AS Descripcion
            FROM DQS_MIP_CODIGOS
            WHERE CIA = @Cia AND TipoServicio = @TipoServicio
              AND (Codigo LIKE @Prefijo + '%' OR [Descripción] LIKE @Prefijo + '%')
            ORDER BY Descripcion ASC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var filas = await connection.QueryAsync<CodigoFila>(sql,
            new { Cia = ciaCatalogo, TipoServicio = tipoServicio, Cantidad = cantidad, Prefijo = prefijo });
        return filas.ToList();
    }
}
