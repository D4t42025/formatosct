using Microsoft.Extensions.Configuration;

namespace Contingencia.Infrastructure.Almacenamiento;

// Carpeta raíz donde la app guarda los archivos que suben los usuarios (formatos, soportes, firmas): por defecto
// <carpeta de la app>\App_Data (igual que hasta ahora). En producción conviene sacarla de la carpeta de publicación con
// "Almacenamiento:RutaBase" (variable de entorno Almacenamiento__RutaBase), p. ej. D:\DatosContingencia, para que un nuevo
// despliegue no borre ni pise los archivos. Debajo de esa raíz sigue la misma estructura: mediauploader\formatos, \contingencias,
// \Files-Gestion-Usuarios\Firmas... (las rutas guardadas en base de datos son relativas a ella).
public static class RutaAlmacenamiento
{
    public static string Base(IConfiguration configuracion, string carpetaDeLaApp)
    {
        var configurada = configuracion["Almacenamiento:RutaBase"];
        return string.IsNullOrWhiteSpace(configurada)
            ? Path.Combine(carpetaDeLaApp, "App_Data")
            : Path.GetFullPath(configurada.Trim());
    }
}
