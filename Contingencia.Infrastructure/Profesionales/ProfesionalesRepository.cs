using System.Data;
using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.Profesionales;

public class ProfesionalesRepository(IStoredProcedureExecutor executor) : IProfesionalesRepository
{
    public Task<DataTable> ConsultarAsync(string cia, string? busqueda, string? profesion, string? sede, string? estado) =>
        executor.RecuperarDataTableAsync("sp_DQS_MIP_ProfesionalesConsultar", cia, busqueda, profesion, sede, estado);

    public Task<DataTable> ObtenerAsync(string cia, int idProfesional) =>
        executor.RecuperarDataTableAsync("sp_DQS_MIP_ProfesionalesObtener", cia, idProfesional);

    public Task<DataTable> InsertarAsync(
        string cia, string? tipoDoc, string noIdentificacion, string? apellidos, string? nombres,
        string? profesion, string? sede, string? servicio, string? correo, string? celular,
        string? factivacion, string? firma, string? usuario, string? idEmpresa, string? clave) =>
        executor.RecuperarDataTableAsync("sp_DQS_MIP_ProfesionalesInsertar",
            cia, tipoDoc, noIdentificacion, apellidos, nombres, profesion, sede, servicio,
            correo, celular, factivacion, firma, usuario, idEmpresa, clave);

    public Task<DataTable> ActualizarAsync(
        string cia, int idProfesional, string? tipoDoc, string noIdentificacion, string? apellidos, string? nombres,
        string? profesion, string? sede, string? servicio, string? correo, string? celular,
        string? factivacion, string? firma, string? usuario, string? idEmpresa, string? clave) =>
        executor.RecuperarDataTableAsync("sp_DQS_MIP_ProfesionalesActualizar",
            cia, idProfesional, tipoDoc, noIdentificacion, apellidos, nombres, profesion, sede, servicio,
            correo, celular, factivacion, firma, usuario, idEmpresa, clave);

    public Task<DataTable> CambiarEstadoAsync(string cia, int idProfesional, bool estado, string? usuario) =>
        executor.RecuperarDataTableAsync("sp_DQS_MIP_ProfesionalesCambiarEstado", cia, idProfesional, estado, usuario);

    public Task<DataTable> ResumenAsync(string cia) =>
        executor.RecuperarDataTableAsync("sp_DQS_MIP_ProfesionalesResumen", cia);

    public Task<DataTable> ListasAsync(string cia) =>
        executor.RecuperarDataTableAsync("sp_DQS_MIP_ProfesionalesListas", cia);
}
