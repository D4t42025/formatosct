namespace Contingencia.Application.Contingencias;

// Textos de resumen de una tecnología, compartidos por la Revisión del wizard
// (NuevaContingencia.razor) y el detalle de ConsultarNuevaContingencia.razor para que ambas
// pantallas digan exactamente lo mismo.
public static class ResumenTecnologia
{
    public const string Medicamento = "Medicamento";

    public static string Detalle(TecnologiaContingencia t) =>
        t.TipoTecnologia == Medicamento
            ? $"DCI: {t.TecPrincipioActivo} — {t.TecDosis}, {t.TecFormaFarmaceutica}, {t.TecViaAdministracion}"
            : $"{t.TecCodigo} — {t.TecNombre}";

    public static string Posologia(TecnologiaContingencia t)
    {
        var indicacion = string.IsNullOrWhiteSpace(t.PosIndicacionEspecial) ? "" : $" — {t.PosIndicacionEspecial}";
        return t.TipoTecnologia == Medicamento
            ? $"Dosis {t.PosCantidadFormulada} {t.PosCantidadFormuladaUnidad}, cada {t.PosCadaFrecuencia} {t.PosFrecuencia}, cantidad total {t.PosCantidadTotal}, por {t.PosDuracionTratamiento} {t.PosDuracionUnidad}{indicacion}"
            : $"Cantidad total {t.PosCantidadTotal}, duración {t.PosDuracionTratamiento} {t.PosDuracionUnidad}{indicacion}";
    }
}
