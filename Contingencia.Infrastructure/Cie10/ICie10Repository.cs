namespace Contingencia.Infrastructure.Cie10;

public record Cie10Fila(string Codigo, string Descripcion, string Marca);

public interface ICie10Repository
{
    Task<IReadOnlyList<Cie10Fila>> BuscarAsync(string prefijo, int cantidad);
}
