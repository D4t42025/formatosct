using System.Data;
using Contingencia.Infrastructure.Filtros;
using Contingencia.Infrastructure.Juntas;
using Contingencia.Infrastructure.Mipres;

namespace Contingencia.Application.Juntas;

public class JuntasService(
    IJuntaProfesionalApiClient apiClient,
    IFiltrosRepository filtrosRepository,
    IJuntasRepository juntasRepository,
    IActaPdfBuilder actaPdfBuilder,
    IFirmaImagenService firmaImagenService) : IJuntasService
{
    // El legado (api(), llamado una vez por día del rango) resuelve NIT/TOKEN por
    // sp_DQS_ConsultaFiltros en CADA llamada y espera cada respuesta HTTP antes de pedir la
    // siguiente — un rango de 2 semanas son ~14 consultas SQL redundantes (siempre el mismo
    // resultado dentro de una misma búsqueda) más ~14 llamadas HTTP en fila, cada una de
    // hasta varios segundos. Acá se resuelve una sola vez y las llamadas HTTP se disparan en
    // paralelo (tope de 8 a la vez, para no saturar la API real del gobierno) — mismos
    // resultados, mucho más rápido. Pedido explícito del usuario ("la consulta debe ser lo
    // mas rapida posible").
    private const int MaxLlamadasEnParalelo = 8;

    public async Task<ResultadoConsultaJuntas> ConsultarAsync(
        string cia, string regimen, bool tokenIps,
        DateTime fechaInicio, DateTime fechaFin, string estado,
        string noPrescripcion, string tipoDoc, string numDoc)
    {
        var (nit, token) = await apiClient.ResolverCredencialesAsync(cia, regimen, tokenIps);
        var diasDiferencia = (fechaFin.Date - fechaInicio.Date).Days;

        List<Func<Task<List<JuntaProfesionalDetalle>>>> tareas;
        if (diasDiferencia == 0)
        {
            if (!string.IsNullOrWhiteSpace(noPrescripcion))
            {
                // Réplica del Split por comas/espacios/saltos de línea de
                // btnconsultar_ServerClick: admite varios números de prescripción.
                var numeros = noPrescripcion
                    .Replace(" ", ",").Replace("\r\n", ",").Replace("\n", ",")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                tareas = numeros
                    .Select(numero => (Func<Task<List<JuntaProfesionalDetalle>>>)(() =>
                        ConsultarUnaAsync(nit, token, estado, fechaInicio.ToString("yyyy-MM-dd"), numero.Trim(), "", "")))
                    .ToList();
            }
            else
            {
                tareas = [() => ConsultarUnaAsync(nit, token, estado, fechaInicio.ToString("yyyy-MM-dd"), "", tipoDoc, numDoc)];
            }
        }
        else
        {
            // Réplica fiel: en un rango de varios días el filtro de No Prescripción se
            // ignora POR COMPLETO — mismo comportamiento exacto de btnconsultar_ServerClick,
            // no una simplificación de esta migración.
            tareas = Enumerable.Range(0, diasDiferencia)
                .Select(i => fechaInicio.Date.AddDays(i).ToString("yyyy-MM-dd"))
                .Select(fecha => (Func<Task<List<JuntaProfesionalDetalle>>>)(() =>
                    ConsultarUnaAsync(nit, token, estado, fecha, "", tipoDoc, numDoc)))
                .ToList();
        }

        var listas = await EjecutarEnParaleloAsync(tareas);
        return new ResultadoConsultaJuntas(listas.SelectMany(x => x).ToList());
    }

    private static async Task<List<List<T>>> EjecutarEnParaleloAsync<T>(IReadOnlyList<Func<Task<List<T>>>> tareas)
    {
        using var semaforo = new SemaphoreSlim(MaxLlamadasEnParalelo);
        var envueltas = tareas.Select(async fabrica =>
        {
            await semaforo.WaitAsync();
            try
            {
                return await fabrica();
            }
            finally
            {
                semaforo.Release();
            }
        });

        return (await Task.WhenAll(envueltas)).ToList();
    }

    private async Task<List<JuntaProfesionalDetalle>> ConsultarUnaAsync(
        string nit, string token, string estado, string fecha, string noPrescripcion, string tipoDoc, string numDoc)
    {
        try
        {
            var items = !string.IsNullOrWhiteSpace(noPrescripcion)
                ? await apiClient.ConsultarPorPrescripcionAsync(nit, token, noPrescripcion)
                : await apiClient.ConsultarPorFechaAsync(nit, token, fecha, tipoDoc, numDoc);

            return (estado is "" or "%" ? items : items.Where(j => j.EstJM == estado)).ToList();
        }
        catch
        {
            // Intencional, igual que el Catch vacío de api() en JuntaProfesional.aspx.vb: un
            // día o número de prescripción que falle (red, JSON, etc.) no debe tumbar el
            // resto de la consulta — el usuario simplemente no ve esos resultados.
            return [];
        }
    }

    public async Task<byte[]> GenerarActaPdfAsync(string cia, bool tokenIps, string noPrescripcion)
    {
        // Réplica fiel de gvconsulta_OnRowCommand (CommandName="PDF"): estas dos llamadas
        // siempre usan el régimen "Contributivo", sin importar el régimen del filtro de
        // búsqueda que trajo la fila. Las dos llamadas HTTP y la carga del cruce de códigos
        // son independientes entre sí — se disparan en paralelo en vez de una tras otra.
        var (nit, token) = await apiClient.ResolverCredencialesAsync(cia, "Contributivo", tokenIps);

        var tareaActa = apiClient.ConsultarPorPrescripcionAsync(nit, token, noPrescripcion);
        var tareaPrescripcion = apiClient.ConsultarDetallePrescripcionAsync(nit, token, noPrescripcion);
        var tareaCruzar = CargarCruzarAsync();
        await Task.WhenAll(tareaActa, tareaPrescripcion, tareaCruzar);

        var acta = (await tareaActa).LastOrDefault();
        var prescripciones = await tareaPrescripcion;
        var cruzar = await tareaCruzar;

        var datosPrescripcion = prescripciones.LastOrDefault()?.Prescripcion;

        var docid = datosPrescripcion?.NroIDIPS ?? "";
        var codhab = datosPrescripcion?.CodHabIPS ?? "";
        var municipio = BuscarCruzar(cruzar, "CodMunEnt", datosPrescripcion?.CodDANEMunIPS ?? "");
        var nombreprest = BuscarCruzar(cruzar, "NroIDIPS", datosPrescripcion?.NroIDIPS ?? "");
        var dprincipalCodigo = datosPrescripcion?.CodDxPpal ?? "";
        var dprincipal = string.IsNullOrEmpty(dprincipalCodigo)
            ? ""
            : $"{dprincipalCodigo}-{BuscarCruzar(cruzar, "CodDxPpal", dprincipalCodigo)}";
        var ambitoatencion = BuscarCruzar(cruzar, "CodAmbAte", datosPrescripcion?.CodAmbAte ?? "");

        var profesionalesDt = await juntasRepository.ConsultarProfesionalesActaAsync(cia, acta?.NoActa ?? "");
        var filasProfesionales = profesionalesDt.AsEnumerable().ToList();
        // Firma llega desde sp_DQS_ConsultaProfesionalAñadido (parcheado en el legado el
        // 2026-09-25 para traerla) — se descarga en paralelo, una por profesional, igual
        // que el resto de llamadas HTTP de este método.
        var imagenesFirma = await Task.WhenAll(
            filasProfesionales.Select(f => firmaImagenService.DescargarAsync(ObtenerFirma(f))));
        var profesionales = filasProfesionales
            .Zip(imagenesFirma, (f, imagen) => new MiembroJunta(
                Convert.ToString(f["NoIDENTIFICACION"]) ?? "",
                Convert.ToString(f["APELLIDOS Y NOMBRES"]) ?? "",
                Convert.ToString(f["PROFESION"]) ?? "",
                imagen))
            .ToList();

        var datos = new DatosActaPdf(
            NumeroActa: acta?.NoActa ?? "",
            FechaActa: acta?.FechaActa ?? "",
            Modalidad: acta?.Modalidad ?? "",
            FechaExpedicion: acta?.FProceso ?? "",
            JustificacionTecnica: acta?.JustificacionTecnica ?? "",
            Decision: TraducirDecision(acta?.EstJM ?? ""),
            Municipio: municipio,
            CodigoHabilitacion: codhab,
            NombrePrestador: nombreprest,
            Direccion: datosPrescripcion?.DirSedeIPS ?? "",
            Telefono: datosPrescripcion?.TelSedeIPS ?? "",
            DocumentoPaciente: docid,
            PrimerApellido: datosPrescripcion?.PAPaciente ?? "",
            SegundoApellido: datosPrescripcion?.SAPaciente ?? "",
            PrimerNombre: datosPrescripcion?.PNPaciente ?? "",
            SegundoNombre: datosPrescripcion?.SNPaciente ?? "",
            HistoriaClinica: datosPrescripcion?.HPrescripcion ?? "",
            DiagnosticoPrincipal: dprincipal,
            AmbitoAtencion: ambitoatencion,
            Medicamentos: prescripciones.SelectMany(p => p.Medicamentos).ToList(),
            Procedimientos: prescripciones.SelectMany(p => p.Procedimientos).ToList(),
            Dispositivos: prescripciones.SelectMany(p => p.Dispositivos).ToList(),
            ProductosNutricionales: prescripciones.SelectMany(p => p.Productosnutricionales).ToList(),
            ServiciosComplementarios: prescripciones.SelectMany(p => p.ServiciosComplementarios).ToList(),
            Profesionales: profesionales);

        return actaPdfBuilder.Generar(datos);
    }

    // sp_DQS_ConsultaFiltros @INTVALIDA=138 ("consulta maestro Mipres" en el legado, cacheada
    // ahí en Session("DTX")): CODIGO/DESCRIPCION/CAMPO desde DQS_MIP_CRUZAR, tabla de cruce
    // genérica usada para traducir decenas de códigos distintos (municipio, IPS, diagnóstico,
    // ámbito de atención, vía de administración, forma farmacéutica, CUPS, dispositivo,
    // servicio complementario...). El nombre de "Campo" en la tabla real no siempre coincide
    // en mayúsculas/minúsculas con el que usa el código legado (ej. "NroIdIps" vs "NroIDIPS",
    // "CODCUPS" vs "CodCUPS") — se compara sin distinguir mayúsculas, igual que hace SQL
    // Server con la collation de esta base (si no, el legado nunca habría encontrado nada).
    private async Task<ILookup<string, (string Codigo, string Descripcion)>> CargarCruzarAsync()
    {
        var dt = await filtrosRepository.ConsultarAsync("", 138);
        return dt.AsEnumerable()
            .Select(f => (
                Campo: Convert.ToString(f["CAMPO"]) ?? "",
                Codigo: Convert.ToString(f["CODIGO"]) ?? "",
                Descripcion: Convert.ToString(f["DESCRIPCION"]) ?? ""))
            .ToLookup(x => x.Campo, x => (x.Codigo, x.Descripcion), StringComparer.OrdinalIgnoreCase);
    }

    private static string BuscarCruzar(ILookup<string, (string Codigo, string Descripcion)> cruzar, string campo, string codigo)
    {
        if (string.IsNullOrEmpty(codigo))
        {
            return "";
        }

        // Réplica de dtX.Select("Codigo = 'x' and Campo ='y'").Rows(0): toma la primera
        // coincidencia y, si no hay ninguna, deja el valor vacío (mismo Catch vacío del
        // legado en cada uno de estos cruces).
        return cruzar[campo].FirstOrDefault(x => x.Codigo == codigo).Descripcion ?? "";
    }

    // Réplica de FirmaActa.RutaDeFila en el legado: la columna Firma solo llega si ya se
    // aplicó el parche del procedimiento (sp_DQS_ConsultaProfesionalAñadido, 2026-09-25).
    // Mientras tanto el acta sigue saliendo como antes en vez de reventar.
    private static string? ObtenerFirma(DataRow fila) =>
        fila.Table.Columns.Contains("Firma") ? Convert.ToString(fila["Firma"]) : null;

    public Task RegistrarCargueAsync(string cia, string prescripcion, string tipoTecnologia, string consecutivo) =>
        juntasRepository.RegistrarCargueAsync(cia, prescripcion, tipoTecnologia, consecutivo);

    private static string TraducirDecision(string codigo) => codigo switch
    {
        "1" => "No requiere junta de profesionales",
        "2" => "Requiere junta de profesionales y pendiente evaluación",
        "3" => "Evaluada por la junta de profesionales y fue aprobada",
        "4" => "Evaluada por la junta de profesionales y no fue aprobada",
        _ => "",
    };
}
