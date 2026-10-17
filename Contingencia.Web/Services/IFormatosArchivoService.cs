namespace Contingencia.Web.Services;

public interface IFormatosArchivoService
{
    string RutaFisica(string cia, string radicado, string extension);
    Task GuardarAsync(string rutaFisica, Stream contenido);
    bool Existe(string rutaFisica);
}
