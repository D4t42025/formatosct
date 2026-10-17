using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Contingencia.Infrastructure.Data;

// Port exacto de Imperium.Infrastructure.Data.StoredProcedureExecutor (Recobros): mismo
// mecanismo genérico (DeriveParameters por posición), sin conocer nombres de parámetro de
// ningún SP en particular — no hay nada específico de Recobros que adaptar aquí.
public class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private static readonly ConcurrentDictionary<string, SqlParameter[]> ParametrosPorProcedimiento = new();

    private readonly string _connectionString;

    public StoredProcedureExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DQUALITY")
            ?? throw new InvalidOperationException("Falta configurar la cadena de conexión 'DQUALITY'.");
    }

    public async Task<DataTable> RecuperarDataTableAsync(string procedimiento, params object?[] parametros)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = CrearComando(connection, procedimiento, parametros);

        var dataTable = new DataTable();
        await using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);
        return dataTable;
    }

    public async Task<DataTable> RecuperarDataTableAsync(string procedimiento, TimeSpan tiempoEspera, object?[] parametros)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = CrearComando(connection, procedimiento, parametros);
        command.CommandTimeout = (int)tiempoEspera.TotalSeconds;

        var dataTable = new DataTable();
        await using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);
        return dataTable;
    }

    public async Task<bool> EjecutarProcedimientoAsync(string procedimiento, params object?[] parametros)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = CrearComando(connection, procedimiento, parametros);
        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<DataTable> RecuperarDataTablePorSqlAsync(string sql, params (string nombre, object? valor)[] parametros)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        foreach (var (nombre, valor) in parametros)
        {
            command.Parameters.AddWithValue(nombre, (object?)valor ?? DBNull.Value);
        }

        var dataTable = new DataTable();
        await using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);
        return dataTable;
    }

    private SqlCommand CrearComando(SqlConnection connection, string procedimiento, object?[] parametros)
    {
        var command = new SqlCommand(procedimiento, connection) { CommandType = CommandType.StoredProcedure };

        var plantilla = ParametrosPorProcedimiento.GetOrAdd(procedimiento, _ => DerivarParametros(procedimiento));
        if (plantilla.Length != parametros.Length)
        {
            throw new InvalidOperationException(
                $"'{procedimiento}' espera {plantilla.Length} parámetro(s) y se recibieron {parametros.Length}.");
        }

        for (var i = 0; i < plantilla.Length; i++)
        {
            var clon = (SqlParameter)((ICloneable)plantilla[i]).Clone();
            clon.Value = parametros[i] ?? DBNull.Value;
            command.Parameters.Add(clon);
        }

        return command;
    }

    private SqlParameter[] DerivarParametros(string procedimiento)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(procedimiento, connection) { CommandType = CommandType.StoredProcedure };
        connection.Open();
        SqlCommandBuilder.DeriveParameters(command);

        return command.Parameters
            .Cast<SqlParameter>()
            .Skip(1)
            .ToArray();
    }
}
