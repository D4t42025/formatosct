using Contingencia.Infrastructure.Dci;

namespace Contingencia.Application.Dci;

public class DciService(IDciRepository repository) : IDciService
{
    public Task<IReadOnlyList<string>> BuscarAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? Task.FromResult<IReadOnlyList<string>>([])
            : repository.BuscarAsync(texto, 100);

    public Task<bool> ExisteAsync(string denominacion) =>
        string.IsNullOrWhiteSpace(denominacion) ? Task.FromResult(false) : repository.ExisteAsync(denominacion);
}
