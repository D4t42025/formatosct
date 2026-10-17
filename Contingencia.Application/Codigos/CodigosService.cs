using Contingencia.Infrastructure.Codigos;

namespace Contingencia.Application.Codigos;

public class CodigosService(ICodigosRepository repository) : ICodigosService
{
    public Task<IReadOnlyList<CodigoFila>> BuscarAsync(string ciaCatalogo, string tipoServicio, string prefijo) =>
        string.IsNullOrWhiteSpace(prefijo) || string.IsNullOrWhiteSpace(tipoServicio)
            ? Task.FromResult<IReadOnlyList<CodigoFila>>([])
            : repository.BuscarAsync(ciaCatalogo, tipoServicio, prefijo, 100);
}
