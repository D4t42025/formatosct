using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Contingencia.Infrastructure.Dci;

// Tabla DQS_MIP_DCI (6.607 filas confirmadas en DQUALITYREC): catálogo de denominaciones
// comunes internacionales (DCI) + concentración, sin código propio (a diferencia de
// DQS_MIP_CODIGOS/DQS_REC_CARGUECIE10) — el usuario lo indicó explícitamente. Columna real
// `[Descripcion DCI Concentracion]` (máximo 965 caracteres reales), con nombre entre
// corchetes por el espacio. La mayoría de los valores reales empiezan con "[" (ej.
// "[ACETAMINOFEN] 100mg/1ml"), así que a diferencia de los demás catálogos (que buscan por
// prefijo) acá se busca por contención (`LIKE '%texto%'`) — un prefijo literal dejaría casi
// todo fuera de la búsqueda. Se filtra `[Habilitado MIPRES] = '1'` (5.467 de 6.607 filas).
// Sin SP existente para esto — SQL directo, mismo patrón que Cie10Repository/CodigosRepository.
public class DciRepository(IConfiguration configuration) : IDciRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DQUALITY")
        ?? throw new InvalidOperationException("Falta configurar la cadena de conexión 'DQUALITY'.");

    public async Task<IReadOnlyList<string>> BuscarAsync(string texto, int cantidad)
    {
        const string sql = """
            SELECT DISTINCT TOP (@Cantidad) [Descripcion DCI Concentracion]
            FROM DQS_MIP_DCI
            WHERE [Habilitado MIPRES] = '1'
              AND [Descripcion DCI Concentracion] LIKE '%' + @Texto + '%'
            ORDER BY [Descripcion DCI Concentracion] ASC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var filas = await connection.QueryAsync<string>(sql, new { Cantidad = cantidad, Texto = texto });
        return filas.ToList();
    }

    public async Task<bool> ExisteAsync(string denominacion)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM DQS_MIP_DCI
                WHERE [Habilitado MIPRES] = '1' AND [Descripcion DCI Concentracion] = @Denominacion
            ) THEN 1 ELSE 0 END
            """;

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<bool>(sql, new { Denominacion = denominacion });
    }
}
