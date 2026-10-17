namespace Contingencia.Application.Afiliados;

public record DatosAfiliado(
    string Nombres, string Apellidos, string Sexo, string? FechaNacimiento,
    string? Municipio, string? Departamento, string? Telefono, string? Regimen, string? EstadoServicio);

public record ResultadoConsultaAfiliado(bool Exito, string? Mensaje, DatosAfiliado? Datos);

public interface IAfiliadoService
{
    // urlWs: ParametrosCia.UrlAfiliados de la CIA de la sesión (null = el de Sanitas).
    Task<ResultadoConsultaAfiliado> ConsultarAsync(string tipoDocumento, string numeroDocumento, string? urlWs = null);
}
