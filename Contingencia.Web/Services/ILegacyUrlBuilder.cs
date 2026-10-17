namespace Contingencia.Web.Services;

// Arma URLs absolutas hacia páginas .aspx del proyecto legado que todavía no se han
// migrado. En producción, si ambas apps quedan bajo el mismo dominio (reverse proxy),
// "Legacy:BaseUrl" puede quedar vacío y las rutas relativas funcionan igual.
public interface ILegacyUrlBuilder
{
    string Url(string rutaRelativa);
}
