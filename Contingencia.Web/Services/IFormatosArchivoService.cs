namespace Contingencia.Web.Services;

// Antes exponía RutaFisica(...) (una ruta en disco que el endpoint /api/formatos/archivo le
// pasaba directo a Results.File) — con FormatosArchivoServiceAzure (CIAs que migran a Azure
// Blob Storage, ver FormatosArchivoServiceSelector) ya no existe una ruta de archivo local
// para esas CIAs, así que el contrato trabaja siempre con cia/radicado/extensión y el
// endpoint abre el contenido como stream sin importar de dónde venga (disco o blob).
public interface IFormatosArchivoService
{
    Task GuardarAsync(string cia, string radicado, string extension, Stream contenido);
    Task<Stream?> AbrirAsync(string cia, string radicado, string extension);
    bool Eliminar(string cia, string radicado, string extension);
    bool Existe(string cia, string radicado, string extension);
}
