using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Contingencia.Infrastructure.Cie10;

// Tabla DQS_REC_CARGUECIE10: vive en la misma base física DQUALITY compartida con Recobros,
// pero solo tenía repositorio allá (Imperium.Infrastructure/Liquidacion/
// LiquidacionAutocompleteRepository.BuscarCie10Async). Se porta la misma consulta (SQL
// directo, no SP — no existe ningún SP de CIE-10 en ninguna de las dos soluciones), con la
// mejora de devolver columnas separadas en vez de un solo string concatenado.
public class Cie10Repository(IConfiguration configuration) : ICie10Repository
{
    private readonly string _connectionString = configuration.GetConnectionString("DQUALITY")
        ?? throw new InvalidOperationException("Falta configurar la cadena de conexión 'DQUALITY'.");

    public async Task<IReadOnlyList<Cie10Fila>> BuscarAsync(string prefijo, int cantidad)
    {
        const string sql = """
            SELECT DISTINCT TOP (@Cantidad)
                   CAST([Código] AS VARCHAR(20)) AS Codigo, [Descripción] AS Descripcion, Marca
            FROM DQS_REC_CARGUECIE10
            WHERE (Código LIKE @Prefijo + '%' OR [Descripción] LIKE @Prefijo + '%')
            ORDER BY Descripcion ASC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var filas = await connection.QueryAsync<Cie10Fila>(sql, new { Cantidad = cantidad, Prefijo = prefijo });
        return filas.ToList();
    }
}
