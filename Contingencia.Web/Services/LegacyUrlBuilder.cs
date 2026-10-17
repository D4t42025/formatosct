namespace Contingencia.Web.Services;

public class LegacyUrlBuilder(IConfiguration configuration) : ILegacyUrlBuilder
{
    public string Url(string rutaRelativa)
    {
        var baseUrl = configuration["Legacy:BaseUrl"];
        return string.IsNullOrWhiteSpace(baseUrl)
            ? rutaRelativa
            : $"{baseUrl.TrimEnd('/')}/{rutaRelativa.TrimStart('/')}";
    }
}
