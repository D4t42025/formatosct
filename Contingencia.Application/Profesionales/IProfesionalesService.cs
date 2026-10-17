using Contingencia.Infrastructure.Profesionales;

namespace Contingencia.Application.Profesionales;

public interface IProfesionalesService
{
    Task<List<ProfesionalMip>> ConsultarAsync(string cia, string? busqueda, string? profesion, string? sede, string? estado);

    Task<ProfesionalMip?> ObtenerAsync(string cia, int idProfesional);

    // idProfesional null o 0 = alta; con valor = edición. usuarioSesion/idEmpresa son los de
    // quien está usando la pantalla — de ahí sale la plantilla (ROL, TOKEN, Cargo...) para el
    // usuario nuevo, si hay que crearlo (ver sp_DQS_MIP_ProfesionalesSincronizarUsuario).
    Task<ResultadoGuardarProfesional> GuardarAsync(
        string cia, int? idProfesional, SolicitudProfesional solicitud, string usuarioSesion, string? idEmpresa);

    Task<ResultadoOperacion> CambiarEstadoAsync(string cia, int idProfesional, bool activar, string usuarioSesion);

    Task<ResumenProfesionales> ObtenerResumenAsync(string cia);

    Task<(List<string> Profesiones, List<string> Sedes)> ObtenerListasAsync(string cia);
}
