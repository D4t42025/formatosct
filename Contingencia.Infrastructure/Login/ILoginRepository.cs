using System.Data;

namespace Contingencia.Infrastructure.Login;

public interface ILoginRepository
{
    Task<DataTable> ConsultarUsuarioAsync(string usuario, string clave);
    Task<DataTable> ConsultarModulosPorRolAsync(string cia, string rol);
    Task<bool> ActualizarMarcaUsuarioAsync(string cia, string usuario, int intValida);
    Task<bool> RegistrarLogueoAsync(string cia, string usuarioApp, string log);
    Task<bool> ActualizarPasswordAsync(string email, string claveNueva);

    // Cambio de clave del propio usuario (sp_MIP_CambiarClaveUsuario): verifica la clave actual en la base.
    Task<(bool Exito, string Mensaje)> CambiarClaveAsync(string usuario, string claveActual, string claveNueva);
}
