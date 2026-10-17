using System.Data;

namespace Contingencia.Infrastructure.Profesionales;

// Los 7 procedimientos que MIP_PROFESIONALES.aspx.vb llama directamente (2026-09-25, ver
// BASE_DE_DATOS.md sección "Página 10"). sp_DQS_MIP_ProfesionalesResolverUsuario y
// sp_DQS_MIP_ProfesionalesSincronizarUsuario son helpers internos que el legado tampoco
// llama directo desde la página — Insertar/Actualizar los invocan por su cuenta dentro de
// la misma transacción — así que no tienen método propio acá.
public interface IProfesionalesRepository
{
    Task<DataTable> ConsultarAsync(string cia, string? busqueda, string? profesion, string? sede, string? estado);

    Task<DataTable> ObtenerAsync(string cia, int idProfesional);

    Task<DataTable> InsertarAsync(
        string cia, string? tipoDoc, string noIdentificacion, string? apellidos, string? nombres,
        string? profesion, string? sede, string? servicio, string? correo, string? celular,
        string? factivacion, string? firma, string? usuario, string? idEmpresa, string? clave);

    Task<DataTable> ActualizarAsync(
        string cia, int idProfesional, string? tipoDoc, string noIdentificacion, string? apellidos, string? nombres,
        string? profesion, string? sede, string? servicio, string? correo, string? celular,
        string? factivacion, string? firma, string? usuario, string? idEmpresa, string? clave);

    Task<DataTable> CambiarEstadoAsync(string cia, int idProfesional, bool estado, string? usuario);

    Task<DataTable> ResumenAsync(string cia);

    Task<DataTable> ListasAsync(string cia);
}
