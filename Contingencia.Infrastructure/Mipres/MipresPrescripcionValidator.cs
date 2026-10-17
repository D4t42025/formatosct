using Contingencia.Infrastructure.Filtros;

namespace Contingencia.Infrastructure.Mipres;

// Réplica de validapres() en JuntasFormatoCntCO2.aspx.vb (el modal "Gestionar formato" del
// legado): antes de aceptar el estado "Gestionado" valida el número de "No Prescripción"
// contra la API real del gobierno (wsmipres.sispro.gov.co), probando primero el token de
// régimen Contributivo y, si no aparece ahí, el de Subsidiado. Es una dependencia externa
// real — a diferencia del legado (sin Try/Catch alrededor de esta llamada, un fallo de red
// tumbaba la página entera), acá cualquier excepción se deja propagar hacia
// FormatosService.GestionarAsync, que sí la atrapa para no tumbar el circuito de Blazor.
public class MipresPrescripcionValidator(IFiltrosRepository filtrosRepository, HttpClient httpClient) : IMipresPrescripcionValidator
{
    public async Task<bool> EsValidaAsync(string cia, string noPrescripcion)
    {
        return await ValidaConRegimenAsync(cia, noPrescripcion, "Contributivo")
            || await ValidaConRegimenAsync(cia, noPrescripcion, "Subsidiado");
    }

    private async Task<bool> ValidaConRegimenAsync(string cia, string noPrescripcion, string regimen)
    {
        // sp_DQS_ConsultaFiltros con @INTVALIDA=136 devuelve NIT,TOKEN desde DQS_MIP_TOKEN
        // (WHERE Cia=@cia AND Regimen=@regimen AND Detalle=@opcion) — mismos nombres de
        // columna que lee dt.Rows(0).Item(0)/Item(1) en el legado.
        var dt = await filtrosRepository.ConsultarAsync(cia, 136, opcion: "APIPRESCRIPCION", regimen: regimen);
        if (dt.Rows.Count == 0)
        {
            return false;
        }

        var nit = Convert.ToString(dt.Rows[0]["NIT"]) ?? "";
        var token = Convert.ToString(dt.Rows[0]["TOKEN"]) ?? "";
        var url = $"https://wsmipres.sispro.gov.co/WSMIPRESNOPBS/api/PrescripcionXNumero/{nit}/{token}/{noPrescripcion}";

        var respuesta = await httpClient.GetStringAsync(url);
        return respuesta.Contains(noPrescripcion, StringComparison.Ordinal);
    }
}
