using System.Data;
using System.Threading;
using Contingencia.Infrastructure.Filtros;
using Contingencia.Infrastructure.Juntas;
using Contingencia.Infrastructure.Mipres;
using Microsoft.Extensions.Caching.Memory;

namespace Contingencia.Application.Juntas;

public class JuntasService(
    IJuntaProfesionalApiClient apiClient,
    IFiltrosRepository filtrosRepository,
    IJuntasRepository juntasRepository,
    IActaPdfBuilder actaPdfBuilder,
    IFirmaImagenService firmaImagenService,
    IMemoryCache cache) : IJuntasService
{
    // El legado (api(), llamado una vez por día del rango) resuelve NIT/TOKEN por
    // sp_DQS_ConsultaFiltros en CADA llamada y espera cada respuesta HTTP antes de pedir la
    // siguiente — un rango de 2 semanas son ~14 consultas SQL redundantes (siempre el mismo
    // resultado dentro de una misma búsqueda) más ~14 llamadas HTTP en fila, cada una de
    // hasta varios segundos. Acá se resuelve una sola vez y las llamadas HTTP se disparan en
    // paralelo (tope de 15 a la vez — subido desde 8 porque un rango de un mes seguía
    // sintiéndose lento; el techo real sigue siendo la latencia de la API del gobierno,
    // 300ms–30s por llamada, no esta app) — mismos resultados, mucho más rápido. Pedido
    // explícito del usuario ("la consulta debe ser lo mas rapida posible").
    private const int MaxLlamadasEnParalelo = 15;

    // Cache corta (en memoria del proceso) de la respuesta CRUDA de cada día/prescripción,
    // antes de filtrar por Estado — el filtro de Estado se aplica en memoria sobre lo cacheado,
    // así que cambiar el combo de Estado y volver a "Consultar" sobre el mismo rango de fechas
    // no vuelve a golpear la API del gobierno para nada (caso de uso muy común: el usuario
    // ajusta el filtro y repite la consulta). TTL corto a propósito: es información que puede
    // cambiar (nuevas actas, nuevos cargues) y no queremos servir datos viejos por mucho rato.
    private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(3);

    public async Task<ResultadoConsultaJuntas> ConsultarAsync(
        string cia, string regimen, bool tokenIps,
        DateTime fechaInicio, DateTime fechaFin, string estado,
        string noPrescripcion, string tipoDoc, string numDoc,
        Func<IReadOnlyList<JuntaProfesionalDetalle>, int, int, Task>? onProgreso = null)
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

        var listas = await EjecutarEnParaleloAsync(tareas, onProgreso);
        return new ResultadoConsultaJuntas(listas.SelectMany(x => x).ToList());
    }

    private static async Task<List<List<T>>> EjecutarEnParaleloAsync<T>(
        IReadOnlyList<Func<Task<List<T>>>> tareas,
        Func<IReadOnlyList<T>, int, int, Task>? onProgreso = null)
    {
        using var semaforo = new SemaphoreSlim(MaxLlamadasEnParalelo);
        var completadas = 0;
        var total = tareas.Count;
        var envueltas = tareas.Select(async fabrica =>
        {
            await semaforo.WaitAsync();
            try
            {
                var resultado = await fabrica();
                var hechasHastaAhora = Interlocked.Increment(ref completadas);
                if (onProgreso is not null)
                {
                    await onProgreso(resultado, hechasHastaAhora, total);
                }

                return resultado;
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
            var claveCache = !string.IsNullOrWhiteSpace(noPrescripcion)
                ? $"juntas:prescripcion:{nit}:{noPrescripcion}"
                : $"juntas:fecha:{nit}:{fecha}:{tipoDoc}:{numDoc}";

            var items = await cache.GetOrCreateAsync(claveCache, async entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = DuracionCache;
                return !string.IsNullOrWhiteSpace(noPrescripcion)
                    ? await apiClient.ConsultarPorPrescripcionAsync(nit, token, noPrescripcion)
                    : await apiClient.ConsultarPorFechaAsync(nit, token, fecha, tipoDoc, numDoc);
            }) ?? [];

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

    public async Task<byte[]> GenerarActaPdfAsync(string cia, bool tokenIps, string noPrescripcion, string regimen)
    {
        // A diferencia del legado (que para generar el acta siempre resolvía credenciales
        // con régimen "Contributivo" fijo, sin importar cuál se usó en el filtro de
        // búsqueda), acá viaja el régimen realmente seleccionado — con la misma validación
        // que ya existe en la UI (Contributivo/Subsidiado), cayendo a "Contributivo" si
        // llega algo distinto (query string manipulada a mano, por ejemplo).
        var regimenSaneado = regimen is "Contributivo" or "Subsidiado" ? regimen : "Contributivo";
        var (nit, token) = await apiClient.ResolverCredencialesAsync(cia, regimenSaneado, tokenIps);

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

        var medicamentos = prescripciones.SelectMany(p => p.Medicamentos).ToList();
        var productosNutricionales = prescripciones.SelectMany(p => p.Productosnutricionales).ToList();
        // Confirmado contra la API en vivo (2026-10-17): "Dosis" es la cantidad cruda (ej.
        // "1000") y "DosisUM" es un código de unidad de medida APARTE (ej. "0168") que se
        // traduce contra DQS_MIP_CRUZAR (Campo='DosisUM', ej. "0168"→"mg") — se combinan acá
        // en un solo texto ("1000 mg") para la celda "Dosis" del acta. "Vía Administración"
        // (CodVA/CodViaAdmon) se traduce igual contra Campo='CodVA' (mismo catálogo que ya
        // usa el wizard de Nueva Contingencia). Si un código no aparece en el catálogo se
        // deja el crudo tal cual en vez de dejar la celda vacía — nunca se pierde
        // información del acta por un cruce que no encontró coincidencia.
        foreach (var m in medicamentos)
        {
            var um = ResolverConRespaldo(cruzar, "DosisUM", m.DosisUM);
            m.Dosis = string.IsNullOrEmpty(um) ? m.Dosis : $"{m.Dosis} {um}".Trim();
            m.CodVA = ResolverConRespaldo(cruzar, "CodVA", m.CodVA);
        }

        foreach (var p in productosNutricionales)
        {
            var um = ResolverConRespaldo(cruzar, "DosisUM", p.DosisUM);
            p.Dosis = string.IsNullOrEmpty(um) ? p.Dosis : $"{p.Dosis} {um}".Trim();
            // Mismo catálogo de vía de administración que Medicamentos (CodVA) — el modelo
            // de Productos Nutricionales solo cambia el nombre de la propiedad
            // (CodViaAdmon en vez de CodVA), no el espacio de códigos.
            p.CodViaAdmon = ResolverConRespaldo(cruzar, "CodVA", p.CodViaAdmon);
        }

        // "Fecha y hora Expedición": la API nunca trae hora en el objeto "acta" (FProceso,
        // FechaActa siempre vienen en 00:00:00 o solo fecha, confirmado en vivo) — la ÚNICA
        // hora real que entrega la API es datosPrescripcion.HPrescripcion (ej. "12:31:53"),
        // así que la fecha de expedición ahora se arma combinando
        // datosPrescripcion.FPrescripcion (fecha) + HPrescripcion (hora real), en vez de
        // acta.FProceso (que nunca tuvo ni podía tener hora).
        var fechaPrescripcion = datosPrescripcion?.FPrescripcion ?? "";
        var horaPrescripcion = TimeSpan.TryParse(datosPrescripcion?.HPrescripcion, out var hp)
            ? hp.ToString(@"hh\:mm")
            : datosPrescripcion?.HPrescripcion ?? "";
        var fechaExpedicion = DateTime.TryParse(fechaPrescripcion, out var fp)
            ? $"{fp:yyyy-MM-dd} {horaPrescripcion}".Trim()
            : acta?.FProceso ?? "";

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
            FechaExpedicion: fechaExpedicion,
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
            // HPrescripcion NO es la historia clínica — es la hora real de la prescripción
            // (ver nota en MipresDatosPrescripcion), ahora usada arriba para
            // "Fecha y hora Expedición". Esta API no expone ningún campo de historia
            // clínica, así que la celda queda vacía en vez de mostrar un valor erróneo
            // (el legado reusaba por error HPrescripcion para esto).
            HistoriaClinica: "",
            DiagnosticoPrincipal: dprincipal,
            AmbitoAtencion: ambitoatencion,
            UsuarioRegimen: regimenSaneado,
            Medicamentos: medicamentos,
            Procedimientos: prescripciones.SelectMany(p => p.Procedimientos).ToList(),
            Dispositivos: prescripciones.SelectMany(p => p.Dispositivos).ToList(),
            ProductosNutricionales: productosNutricionales,
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

    // Variante de BuscarCruzar para Dosis/Vía Administración: si el código no aparece en
    // el catálogo (nombre de Campo no confirmado contra datos reales, o código nuevo que
    // el catálogo aún no tiene), se deja el código crudo en vez de dejar la celda vacía —
    // nunca se pierde información del acta por un cruce que no encontró coincidencia.
    private static string ResolverConRespaldo(ILookup<string, (string Codigo, string Descripcion)> cruzar, string campo, string codigo)
    {
        if (string.IsNullOrEmpty(codigo))
        {
            return codigo;
        }

        var descripcion = BuscarCruzar(cruzar, campo, codigo);
        return string.IsNullOrEmpty(descripcion) ? codigo : descripcion;
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
