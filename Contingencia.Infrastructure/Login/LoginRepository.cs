using System.Data;
using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.Login;

// Equivalente a Login3.aspx.vb: mismos procedimientos almacenados (los mismos objetos de
// base de datos que usa Recobros — ver BASE_DE_DATOS.md, sección "Relación con Recobros"),
// mismo orden de parámetros. Login3 no llama sp_DQS_ActualizaPwd ni sp_opt_consultalogueo
// (no tiene flujo de "olvidé mi clave" activo en la UI actual, ni chequeo de "sesión activa
// en otro equipo" en el propio login) — por eso no están aquí, a diferencia de
// Imperium.Infrastructure.Login.LoginRepository (Recobros).
public class LoginRepository(IStoredProcedureExecutor executor) : ILoginRepository
{
    public Task<DataTable> ConsultarUsuarioAsync(string usuario, string clave) =>
        executor.RecuperarDataTableAsync("sp_DQS_ConsultaUsuario", usuario, clave);

    public Task<DataTable> ConsultarModulosPorRolAsync(string cia, string rol) =>
        executor.RecuperarDataTableAsync("sp_DQS_ConsultaModxRol", cia, rol);

    public Task<bool> ActualizarMarcaUsuarioAsync(string cia, string usuario, int intValida) =>
        executor.EjecutarProcedimientoAsync("sp_DQS_ActualizaMarcaUsuario", cia, usuario, intValida);

    public Task<bool> RegistrarLogueoAsync(string cia, string usuarioApp, string log) =>
        executor.EjecutarProcedimientoAsync("sp_opt_ingresalogueo", cia, usuarioApp, log);

    // Agregado por decisión explícita del usuario para el flujo de "recuperar clave" — mismo
    // SP que ya documenta BASE_DE_DATOS.md de Recobros (sp_DQS_ActualizaPwd), llamado desde
    // GenerarCadena(8) en Login3.aspx.vb (ver PlantillaRecuperacionClave/LoginService).
    public Task<bool> ActualizarPasswordAsync(string email, string claveNueva) =>
        executor.EjecutarProcedimientoAsync("sp_DQS_ActualizaPwd", email, claveNueva);

    // Pantalla "Actualizar contraseña" (migración de ActContraseña.aspx): ver Sql/013_MipCambiarClaveUsuario.sql.
    public async Task<(bool Exito, string Mensaje)> CambiarClaveAsync(string usuario, string claveActual, string claveNueva)
    {
        var dt = await executor.RecuperarDataTableAsync("sp_MIP_CambiarClaveUsuario", usuario, claveActual, claveNueva);
        var fila = dt.Rows[0];
        return (Convert.ToInt32(fila["RESULTADO"]) == 1, Convert.ToString(fila["MENSAJE"]) ?? "");
    }
}
