namespace Contingencia.Infrastructure.Mipres;

// Equivalente a las llamadas a wsmipres.sispro.gov.co/WSMIPRESNOPBS/api/... dentro de
// api() y gvconsulta_OnRowCommand en JuntaProfesional.aspx.vb.
//
// A diferencia del legado (que resuelve NIT/TOKEN de nuevo en cada llamada — una consulta
// SQL por cada día del rango, aunque el resultado es siempre el mismo dentro de una misma
// búsqueda), acá se resuelve UNA sola vez con ResolverCredencialesAsync y se reutiliza en
// todas las llamadas de esa búsqueda — mismo resultado, sin round-trips redundantes a la
// base de datos. Esto es necesario para poder paralelizar las llamadas HTTP en
// JuntasService (pedido explícito del usuario: "la consulta debe ser lo mas rapida
// posible") sin disparar además decenas de consultas SQL en paralelo.
public interface IJuntaProfesionalApiClient
{
    // sp_DQS_ConsultaFiltros @INTVALIDA=136 (NIT/TOKEN de DQS_MIP_TOKEN por CIA+Regimen+
    // Detalle, según tokenIps: APIPRESCRIPCION2/APIPRESCRIPCION).
    Task<(string Nit, string Token)> ResolverCredencialesAsync(string cia, string regimen, bool tokenIps);

    // JuntaProfesionalXFecha (cuando no hay documento de paciente) o JuntaProfesionalXPaciente
    // (cuando sí) — misma bifurcación de api() en el legado. tipoDoc/numDoc vacíos activan la
    // primera variante.
    Task<IReadOnlyList<JuntaProfesionalDetalle>> ConsultarPorFechaAsync(
        string nit, string token, string fecha, string tipoDoc, string numDoc);

    // JuntaProfesional/{nit}/{token}/{noPrescripcion} — usada tanto para la búsqueda por
    // número de prescripción como, con credenciales resueltas en régimen "Contributivo",
    // para obtener los datos del acta al generar el PDF (mismo endpoint, distinto uso).
    Task<IReadOnlyList<JuntaProfesionalDetalle>> ConsultarPorPrescripcionAsync(
        string nit, string token, string noPrescripcion);

    // PrescripcionXNumero/{nit}/{token}/{noPrescripcion} — solo se usa al generar el PDF del
    // acta, con credenciales resueltas en régimen fijo "Contributivo" (réplica fiel de
    // gvconsulta_OnRowCommand, que ignora el régimen seleccionado en el filtro de búsqueda
    // para esta llamada en particular).
    Task<IReadOnlyList<MipresPrescripcion>> ConsultarDetallePrescripcionAsync(
        string nit, string token, string noPrescripcion);
}
