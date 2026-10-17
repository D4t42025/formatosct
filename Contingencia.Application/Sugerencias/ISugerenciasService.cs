namespace Contingencia.Application.Sugerencias;

public interface ISugerenciasService
{
    Task<bool> EnviarAsync(string tipo, string email, string mensaje, string usuario);
}
