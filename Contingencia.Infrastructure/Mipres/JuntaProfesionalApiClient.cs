using System.Text.Json;
using Contingencia.Infrastructure.Filtros;

namespace Contingencia.Infrastructure.Mipres;

public class JuntaProfesionalApiClient(IFiltrosRepository filtrosRepository, HttpClient httpClient) : IJuntaProfesionalApiClient
{
    private const string BaseUrl = "https://wsmipres.sispro.gov.co/WSMIPRESNOPBS/api";

    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FlexibleStringConverterFactory() },
    };

    public async Task<(string Nit, string Token)> ResolverCredencialesAsync(string cia, string regimen, bool tokenIps)
    {
        var opcion = tokenIps ? "APIPRESCRIPCION2" : "APIPRESCRIPCION";
        var dt = await filtrosRepository.ConsultarAsync(cia, 136, opcion: opcion, regimen: regimen);
        if (dt.Rows.Count == 0)
        {
            return ("", "");
        }

        return (Convert.ToString(dt.Rows[0]["NIT"]) ?? "", Convert.ToString(dt.Rows[0]["TOKEN"]) ?? "");
    }

    public async Task<IReadOnlyList<JuntaProfesionalDetalle>> ConsultarPorFechaAsync(
        string nit, string token, string fecha, string tipoDoc, string numDoc)
    {
        // Réplica exacta de api(): solo usa JuntaProfesionalXPaciente cuando AMBOS el tipo y
        // el número de documento vienen informados; de lo contrario cae a JuntaProfesionalXFecha.
        var url = !string.IsNullOrEmpty(numDoc) && !string.IsNullOrEmpty(tipoDoc)
            ? $"{BaseUrl}/JuntaProfesionalXPaciente/{nit}/{fecha}/{token}/{tipoDoc}/{numDoc}"
            : $"{BaseUrl}/JuntaProfesionalXFecha/{nit}/{token}/{fecha}";

        return await ObtenerListaAsync(url);
    }

    public async Task<IReadOnlyList<JuntaProfesionalDetalle>> ConsultarPorPrescripcionAsync(
        string nit, string token, string noPrescripcion)
    {
        var url = $"{BaseUrl}/JuntaProfesional/{nit}/{token}/{noPrescripcion}";
        return await ObtenerListaAsync(url);
    }

    public async Task<IReadOnlyList<MipresPrescripcion>> ConsultarDetallePrescripcionAsync(
        string nit, string token, string noPrescripcion)
    {
        var url = $"{BaseUrl}/PrescripcionXNumero/{nit}/{token}/{noPrescripcion}";
        var json = await httpClient.GetStringAsync(url);
        return JsonSerializer.Deserialize<List<MipresPrescripcion>>(json, JsonOpciones) ?? [];
    }

    private async Task<IReadOnlyList<JuntaProfesionalDetalle>> ObtenerListaAsync(string url)
    {
        var json = await httpClient.GetStringAsync(url);
        var items = JsonSerializer.Deserialize<List<JuntaProfesionalApiItem>>(json, JsonOpciones) ?? [];
        return items.Select(i => i.JuntaProfesional).Where(j => j is not null).Select(j => j!).ToList();
    }
}
