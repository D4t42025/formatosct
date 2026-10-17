using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Contingencia.Infrastructure.Parametricas;

namespace Contingencia.Application.Parametricas;

public class ParametricasService(IParametricasRepository repository) : IParametricasService
{
    public async Task<ILookup<string, ParametricaValor>> CargarAsync()
    {
        var dt = await repository.ConsultarAsync();
        return dt.AsEnumerable()
            .Select(f => (
                Grupo: Convert.ToString(f["GRUPO"]) ?? "",
                Codigo: Convert.ToString(f["CODIGO"]) ?? "",
                Valor: Convert.ToString(f["VALOR"]) ?? ""))
            .ToLookup(x => x.Grupo, x => new ParametricaValor(x.Codigo, x.Valor), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ResultadoCrearParametrica> CrearAsync(string grupo, string valor)
    {
        var valorLimpio = valor.Trim();
        if (string.IsNullOrEmpty(valorLimpio))
        {
            return new ResultadoCrearParametrica(false, "Debe ingresar un nombre.", null);
        }

        var codigo = GenerarCodigo(valorLimpio);
        if (codigo.Length == 0)
        {
            return new ResultadoCrearParametrica(false, "El nombre debe contener al menos una letra o número.", null);
        }

        var (exito, mensaje) = await repository.InsertarAsync(grupo, codigo, valorLimpio);
        return new ResultadoCrearParametrica(exito, mensaje, exito ? new ParametricaValor(codigo, valorLimpio) : null);
    }

    // "Falla eléctrica del generador" -> "FALLA_ELECTRICA_DEL_GENERADOR" — sin tildes, en
    // mayúsculas, con guion bajo entre palabras, mismo estilo que los códigos de la semilla
    // inicial (DIFICULTAD_TECNICA, SIN_SERVICIO_ELECTRICO, etc.).
    private static string GenerarCodigo(string valor)
    {
        var descompuesto = valor.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(caracter);
            }
        }

        var sinTildes = builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        var soloAlfanumerico = Regex.Replace(sinTildes, "[^A-Z0-9]+", "_");
        var sinGuionesRepetidos = Regex.Replace(soloAlfanumerico, "_+", "_").Trim('_');
        return sinGuionesRepetidos.Length > 50 ? sinGuionesRepetidos[..50].Trim('_') : sinGuionesRepetidos;
    }
}
