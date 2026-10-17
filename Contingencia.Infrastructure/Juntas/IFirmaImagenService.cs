namespace Contingencia.Infrastructure.Juntas;

// Las firmas de los profesionales se dibujan/suben desde MIP_PROFESIONALES.aspx (legado,
// todavía no migrado a esta app) y quedan servidas como archivo estático sin autenticación
// bajo ~/MediaUploader/... en ese sitio. Como esta app no comparte disco con el legado, la
// única forma de traer la imagen es pedirla por HTTP.
public interface IFirmaImagenService
{
    // Devuelve los bytes crudos del archivo, o null si no hay ruta, si "Legacy:BaseUrl" no
    // está configurado, o si la descarga falla por cualquier razón (archivo borrado, red,
    // 404...). Nunca lanza: una firma que falta no puede impedir que se imprima el acta.
    Task<byte[]?> DescargarAsync(string? rutaRelativa);
}
