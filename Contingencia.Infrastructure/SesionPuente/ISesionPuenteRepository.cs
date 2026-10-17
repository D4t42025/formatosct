using System.Data;
using Contingencia.Domain.SesionPuente;

namespace Contingencia.Infrastructure.SesionPuente;

public interface ISesionPuenteRepository
{
    Task<Guid> CrearAsync(DatosSesionPuente datos, DataTable modulos, TimeSpan vigencia);

    // Lee y borra el token (de un solo uso). Null si no existe o ya expiró.
    Task<(DatosSesionPuente Datos, IReadOnlyList<string> Modulos)?> ConsumirAsync(Guid token);
}
