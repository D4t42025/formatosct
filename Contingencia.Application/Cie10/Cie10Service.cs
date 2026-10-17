using Contingencia.Infrastructure.Cie10;

namespace Contingencia.Application.Cie10;

public class Cie10Service(ICie10Repository repository) : ICie10Service
{
    public Task<IReadOnlyList<Cie10Fila>> BuscarAsync(string prefijo) =>
        string.IsNullOrWhiteSpace(prefijo)
            ? Task.FromResult<IReadOnlyList<Cie10Fila>>([])
            : repository.BuscarAsync(prefijo, 100);
}
