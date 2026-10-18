namespace Contingencia.Web.Services;

// Servicio hermano de IFormatosArchivoService, pero para el wizard "Nueva Contingencia":
// ese servicio asume un solo archivo por radicado; acá se necesitan varios soportes más
// una firma por radicado. Las rutas que maneja este contrato son siempre RELATIVAS
// (ej. "999/2026.../soporte_1.pdf") — son las que se guardan en SOPORTES_RUTAS_JSON/
// FIRMA_RUTA y las que arma /api/contingencias/archivo; RutaFisica las resuelve a una ruta
// absoluta solo para leer/escribir en disco.
public interface IContingenciaArchivoService
{
    string RutaRelativaSoporte(string cia, string radicado, int indice, string extension);
    string RutaRelativaFirma(string cia, string radicado);
    string RutaFisica(string rutaRelativa);
    Task GuardarAsync(string rutaRelativa, Stream contenido);
    Task GuardarBase64PngAsync(string rutaRelativa, string imagenBase64Png);
    bool Existe(string rutaRelativa);

    // Borrado físico de un soporte/firma puntual (disco) — ver implementación.
    bool Eliminar(string rutaRelativa);
}
