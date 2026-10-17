namespace Contingencia.Application.Dci;

public interface IDciService
{
    Task<IReadOnlyList<string>> BuscarAsync(string texto);

    Task<bool> ExisteAsync(string denominacion);
}
