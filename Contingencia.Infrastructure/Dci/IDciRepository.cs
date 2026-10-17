namespace Contingencia.Infrastructure.Dci;

public interface IDciRepository
{
    Task<IReadOnlyList<string>> BuscarAsync(string texto, int cantidad);

    Task<bool> ExisteAsync(string denominacion);
}
