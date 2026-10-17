using Contingencia.Infrastructure.Codigos;

namespace Contingencia.Application.Codigos;

public interface ICodigosService
{
    // ciaCatalogo: ParametrosCia.CodigosCatalogo de la CIA de la sesión.
    Task<IReadOnlyList<CodigoFila>> BuscarAsync(string ciaCatalogo, string tipoServicio, string prefijo);
}
