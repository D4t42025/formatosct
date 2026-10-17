using System.Data;

namespace Contingencia.Infrastructure.Data;

// Port exacto de Imperium.Infrastructure.Data.IStoredProcedureExecutor (Recobros) — mismo
// contrato genérico, sin lógica propia de ningún módulo, así que no hay nada que adaptar.
public interface IStoredProcedureExecutor
{
    Task<DataTable> RecuperarDataTableAsync(string procedimiento, params object?[] parametros);
    Task<bool> EjecutarProcedimientoAsync(string procedimiento, params object?[] parametros);
    Task<DataTable> RecuperarDataTableAsync(string procedimiento, TimeSpan tiempoEspera, object?[] parametros);
    Task<DataTable> RecuperarDataTablePorSqlAsync(string sql, params (string nombre, object? valor)[] parametros);
}
