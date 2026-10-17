namespace Contingencia.Application.Login;

public record ResultadoCambioClave(bool Exito, string Mensaje);

public interface ICambioClaveService
{
    // Pantalla "Actualizar contraseña" (migración de ActContraseña.aspx). Valida la confirmación y la política
    // de clave del original y luego deja que la base verifique la clave actual y aplique el cambio.
    Task<ResultadoCambioClave> CambiarAsync(string usuario, string claveActual, string claveNueva, string confirmacion);
}
