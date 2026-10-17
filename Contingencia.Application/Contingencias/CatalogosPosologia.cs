namespace Contingencia.Application.Contingencias;

// Listas fijas para los desplegables de Posología del wizard "Nueva Contingencia". Duplica
// (no refactoriza) los valores ya usados en Contingencia.Infrastructure/Juntas/
// ActaPdfBuilder.cs (FrecuenciaAdministracion/FrecuenciaUso/DuracionPeriodo/
// IndicacionEspecial/TipoPrestacion) — esos son códigos MIPRES ya verificados contra el
// legado, cerrados y estables, así que reutilizar los mismos textos es más seguro que
// inventar una lista nueva. Acá se guarda directamente el TEXTO (no el código numérico),
// porque nada fuera de esta tabla necesita decodificarlo de vuelta.
public static class CatalogosPosologia
{
    public static readonly IReadOnlyList<string> TipoPrestacion = ["Única", "Sucesiva"];

    public static readonly IReadOnlyList<string> Frecuencia =
        ["Minuto(s)", "Hora(s)", "Día(s)", "Semana(s)", "Mes(es)", "Año", "Según respuesta al", "Única"];

    public static readonly IReadOnlyList<string> DuracionUnidad =
        ["Minuto(s)", "Hora(s)", "Día(s)", "Semana(s)", "Mes(es)", "Año"];

    public static readonly IReadOnlyList<string> IndicacionEspecial =
    [
        "Administración en dosis única",
        "Administración inmediata",
        "Administrar en Bolo",
        "Administrar en Goteo",
        "Infusión continua",
        "Infusión intermitente",
        "Infusión intermitente simultánea con perfusión de otra solución",
        "Microgoteo",
        "Perfusión",
        "Sin indicación especial",
    ];

    public static readonly IReadOnlyList<string> IndicadorRecurrencia = ["Sí", "No"];
}
