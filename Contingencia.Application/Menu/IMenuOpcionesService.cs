using Contingencia.Domain.Menu;

namespace Contingencia.Application.Menu;

public interface IMenuOpcionesService
{
    IReadOnlyList<AppCard> ObtenerTarjetas();
}
