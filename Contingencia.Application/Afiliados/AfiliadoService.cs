using System.Globalization;
using Contingencia.Infrastructure.Afiliados;

namespace Contingencia.Application.Afiliados;

public class AfiliadoService(IAfiliadoApiClient apiClient) : IAfiliadoService
{
    public async Task<ResultadoConsultaAfiliado> ConsultarAsync(string tipoDocumento, string numeroDocumento, string? urlWs = null)
    {
        if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(numeroDocumento))
        {
            return new ResultadoConsultaAfiliado(false, "Debe seleccionar el tipo e ingresar el número de documento.", null);
        }

        var resultado = await apiClient.ConsultarAsync(tipoDocumento, numeroDocumento.Trim(), urlWs);
        if (!resultado.Exito || resultado.Datos is null)
        {
            return new ResultadoConsultaAfiliado(false, resultado.Mensaje, null);
        }

        var api = resultado.Datos;
        var apellidos = string.Join(" ", new[] { api.Apellido1, api.Apellido2 }.Where(a => !string.IsNullOrWhiteSpace(a)));

        var datos = new DatosAfiliado(
            (api.Nombres ?? "").Trim(),
            apellidos.Trim(),
            (api.Sexo ?? "").Trim().ToUpperInvariant(),
            FormatearFecha(api.FechaNacimiento),
            api.Municipio?.Trim(),
            api.Departamento?.Trim(),
            api.Celular?.Trim(),
            NormalizarRegimen(api.Regimen),
            api.EstadoDelServicio?.Trim());

        return new ResultadoConsultaAfiliado(true, null, datos);
    }

    // El WS devuelve la fecha como "1981-02-04T00:00:00" — se guarda solo la parte de fecha,
    // en el mismo formato "yyyy-MM-dd" que usa el <input type="date"> del wizard.
    private static string? FormatearFecha(string? fechaIso) =>
        DateTime.TryParse(fechaIso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;

    // El WS devuelve "SUBSIDIADO"/"CONTRIBUTIVO" en mayúsculas — se deja solo la primera letra
    // en mayúscula para que se vea igual que el resto de la aplicación, sin inventar un valor
    // si no coincide con ninguno de los conocidos.
    private static string? NormalizarRegimen(string? regimen)
    {
        if (string.IsNullOrWhiteSpace(regimen)) return null;
        var texto = regimen.Trim().ToLowerInvariant();
        return char.ToUpperInvariant(texto[0]) + texto[1..];
    }
}
