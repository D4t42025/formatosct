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
    Task GuardarAsync(string rutaRelativa, Stream contenido);
    Task GuardarBase64PngAsync(string rutaRelativa, string imagenBase64Png);
    bool Existe(string rutaRelativa);

    // Borrado físico (disco) o del blob (Azure) de un soporte/firma puntual — ver implementación.
    bool Eliminar(string rutaRelativa);

    // Antes era RutaFisica(...) (una ruta en disco que /api/contingencias/archivo y
    // /api/contingencias/formulario le pasaban directo a Results.File/File.ReadAllBytesAsync).
    // Con el enrutado dinámico a Azure Blob Storage (ver ContingenciaArchivoServiceSelector)
    // ya no existe siempre una ruta de archivo local, así que ambos endpoints abren el
    // contenido como stream sin importar de dónde venga (disco o blob).
    Task<Stream?> AbrirAsync(string rutaRelativa);
}
