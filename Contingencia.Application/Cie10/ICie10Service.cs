using Contingencia.Infrastructure.Cie10;

namespace Contingencia.Application.Cie10;

public interface ICie10Service
{
    Task<IReadOnlyList<Cie10Fila>> BuscarAsync(string prefijo);
}
