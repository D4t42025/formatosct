using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contingencia.Infrastructure.Mipres;

// La API real de SISPRO no es consistente con los tipos: campos como "Consecutivo",
// "EstJM" o "Modalidad" a veces llegan como número JSON (1, 2) y no como texto ("1", "2")
// — igual que el legado, que los lee con VB de tipado débil (todo se concatena/compara
// como texto sin problema). System.Text.Json es estricto por defecto y tira JsonException
// al intentar meter un número JSON en una propiedad `string`, lo que silenciosamente
// aparentaba "sin resultados" en JuntasService (el mismo Catch vacío que replica el
// legado se comía esa excepción). Este conversor acepta string, número o null para
// cualquier propiedad `string` de los DTOs de esta API — confirmado necesario probando en
// vivo contra la API real (ver BASE_DE_DATOS.md, "Página 8").
public class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var entero)
                ? entero.ToString()
                : reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => null,
            _ => throw new JsonException($"No se pudo convertir el token '{reader.TokenType}' a string."),
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}

public class FlexibleStringConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(string);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        new FlexibleStringConverter();
}
