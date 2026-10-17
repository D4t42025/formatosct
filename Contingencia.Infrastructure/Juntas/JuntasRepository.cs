using System.Data;
using Contingencia.Infrastructure.Data;

namespace Contingencia.Infrastructure.Juntas;

public class JuntasRepository(IStoredProcedureExecutor executor) : IJuntasRepository
{
    public Task<DataTable> ConsultarProfesionalesActaAsync(string cia, string acta) =>
        executor.RecuperarDataTableAsync("sp_DQS_ConsultaProfesionalAñadido", cia, acta);

    public Task<DataTable> BuscarProfesionalesAsync(string cia, string documento, string nombre, string profesion, string servicio) =>
        // Orden real de sp_DQS_ConsultaProfesional: @cia, @doc, @nom, @prof, @sede, @serv.
        // "servicio" (lo que el usuario escribe en el campo "Servicio") viaja como @sede —
        // réplica del bug del legado, ver IJuntasRepository.
        executor.RecuperarDataTableAsync("sp_DQS_ConsultaProfesional", cia, documento, nombre, profesion, servicio, nombre);

    public async Task<bool> AsignarProfesionalAsync(string cia, string acta, string documento)
    {
        try
        {
            return await executor.EjecutarProcedimientoAsync("sp_DQS_AsignaProfesional", cia, acta, documento);
        }
        catch
        {
            // Réplica fiel del Try/Catch de asignaprofesionales() en el legado: cualquier
            // fallo (típicamente, ya estaba asignado) se interpreta como "no se pudo".
            return false;
        }
    }

    public async Task<bool> EliminarProfesionalAsync(string cia, string acta, string documento)
    {
        try
        {
            return await executor.EjecutarProcedimientoAsync("sp_DQS_EliminaProfesional", cia, acta, documento);
        }
        catch
        {
            return false;
        }
    }

    public Task ActualizarEstadoActaAsync(string cia, string acta) =>
        executor.EjecutarProcedimientoAsync("sp_DQS_ActualizaProfesional", cia, acta);

    public Task RegistrarCargueAsync(string cia, string prescripcion, string tipoTecnologia, string consecutivo) =>
        executor.EjecutarProcedimientoAsync("sp_DQS_ActualizaJunta", cia, prescripcion, tipoTecnologia, consecutivo);
}
