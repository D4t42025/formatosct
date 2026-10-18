using Contingencia.Infrastructure.Mipres;

namespace Contingencia.Application.Juntas;

public record ResultadoConsultaJuntas(IReadOnlyList<JuntaProfesionalDetalle> Filas);

public interface IJuntasService
{
    // Réplica de btnconsultar_ServerClick en JuntaProfesional.aspx.vb: si Fecha Inicio y
    // Fecha Fin son el mismo día, consulta una sola vez (por N° prescripción, si se
    // informó — admite varios separados por coma/espacio/salto de línea — o por
    // fecha+documento de paciente). Si son días distintos, recorre CADA día del rango y
    // el filtro de N° Prescripción se ignora por completo (mismo comportamiento exacto
    // del legado, no una simplificación). Cada llamada a la API de SISPRO que falle se
    // descarta en silencio (mismo Catch vacío de api()) sin abortar el resto del rango.
    // "onProgreso" (opcional) se invoca cada vez que termina una llamada individual (un
    // día o un N° de prescripción), con las filas de esa llamada + cuántas de las N
    // llamadas totales ya terminaron — permite a la UI pintar resultados a medida que
    // llegan en vez de esperar a que termine el rango completo (rangos largos como un mes
    // pueden tardar más de un minuto por la latencia propia de la API del gobierno).
    Task<ResultadoConsultaJuntas> ConsultarAsync(
        string cia, string regimen, bool tokenIps,
        DateTime fechaInicio, DateTime fechaFin, string estado,
        string noPrescripcion, string tipoDoc, string numDoc,
        Func<IReadOnlyList<JuntaProfesionalDetalle>, int, int, Task>? onProgreso = null);

    // Réplica del botón "PDF" (gvconsulta_OnRowCommand, CommandName="PDF") en
    // JuntaProfesional.aspx.vb: arma el acta de junta de profesionales de la salud en PDF
    // para una prescripción puntual. "regimen" se usa para resolver credenciales (NIT/TOKEN)
    // y para estampar "Usuario Régimen" en el acta — a diferencia del legado (que siempre
    // usaba Contributivo sin importar el filtro de búsqueda), ahora viaja el régimen
    // realmente elegido en el filtro; por compatibilidad, un valor vacío o inválido cae a
    // "Contributivo" (ver JuntasService.SanearRegimen).
    Task<byte[]> GenerarActaPdfAsync(string cia, bool tokenIps, string noPrescripcion, string regimen);

    // Réplica de la llamada a objbll.actualizajunta() en JuntasCargueDoc.aspx.vb
    // (FileUploadUsingJQuerySelectionMethod), una vez por cada archivo subido con éxito.
    Task RegistrarCargueAsync(string cia, string prescripcion, string tipoTecnologia, string consecutivo);
}
