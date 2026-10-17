using System.Data;

namespace Contingencia.Infrastructure.Parametricas;

public interface IParametricasRepository
{
    Task<DataTable> ConsultarAsync();

    Task<(bool Exito, string Mensaje)> InsertarAsync(string grupo, string codigo, string valor);
}
