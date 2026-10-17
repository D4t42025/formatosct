using System.Text.Json.Serialization;

namespace Contingencia.Infrastructure.Afiliados;

// Forma exacta del JSON del WS de afiliados de EPS Sanitas (confirmada con una captura real
// de Postman del usuario) — nombres de propiedad en mayúsculas tal como los devuelve el
// servicio, no se pueden cambiar.
public class SolicitudConsultaAfiliadoApi
{
    [JsonPropertyName("TIPO_IDENTIFICACION")]
    public string TipoIdentificacion { get; set; } = "";

    [JsonPropertyName("NUMERO_IDENTIFICACION")]
    public string NumeroIdentificacion { get; set; } = "";
}

// Solo se mapean los campos que el wizard realmente necesita — el JSON real trae más
// (PLANES, FAMILIA, NUMERO_DE_CONTRATO_PAC, DIRECCION, "Beneficiario" con la lista de
// dependientes, etc.) que no hacen falta acá; los campos no declarados se ignoran al
// deserializar, no hace falta mapear el objeto completo.
public class RespuestaAfiliadoApi
{
    [JsonPropertyName("TIPO_IDENT")]
    public string? TipoIdent { get; set; }

    [JsonPropertyName("NUM_IDENTIFICACION")]
    public string? NumIdentificacion { get; set; }

    [JsonPropertyName("NOMBRES")]
    public string? Nombres { get; set; }

    [JsonPropertyName("APELLIDO1")]
    public string? Apellido1 { get; set; }

    [JsonPropertyName("APELLIDO2")]
    public string? Apellido2 { get; set; }

    [JsonPropertyName("SEXO")]
    public string? Sexo { get; set; }

    [JsonPropertyName("FECHA_NACIMIENTO")]
    public string? FechaNacimiento { get; set; }

    [JsonPropertyName("MUNICIPIO")]
    public string? Municipio { get; set; }

    [JsonPropertyName("DEPARTAMENTO")]
    public string? Departamento { get; set; }

    [JsonPropertyName("CELULAR")]
    public string? Celular { get; set; }

    [JsonPropertyName("REGIMEN")]
    public string? Regimen { get; set; }

    [JsonPropertyName("ESTADO_DEL_SERVICIO")]
    public string? EstadoDelServicio { get; set; }

    [JsonPropertyName("TIPO_USUARIO")]
    public string? TipoUsuario { get; set; }

    [JsonPropertyName("DESCRIPCION")]
    public string? Descripcion { get; set; }
}

public class RespuestaConsultaAfiliadoApi
{
    [JsonPropertyName("respuesta")]
    public RespuestaAfiliadoApi? Respuesta { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
