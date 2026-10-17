namespace Contingencia.Application.Parametricas;

public record ParametricaValor(string Codigo, string Valor);

public record ResultadoCrearParametrica(bool Exito, string? Mensaje, ParametricaValor? Creado);

public interface IParametricasService
{
    // Trae TODO el catálogo y lo agrupa por GRUPO (ej. "CAUSAL") — mismo patrón que
    // JuntasService.CargarCruzarAsync/BuscarCruzar sobre DQS_MIP_CRUZAR.
    Task<ILookup<string, ParametricaValor>> CargarAsync();

    // Permite crear un valor nuevo en un grupo (ej. una causal que no esté en la semilla
    // inicial) desde la propia aplicación — el código se genera acá a partir del texto que
    // escribe el usuario, no lo elige él.
    Task<ResultadoCrearParametrica> CrearAsync(string grupo, string valor);
}
