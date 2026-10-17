using System.Data;

namespace Contingencia.Domain.Login;

// Mapea las columnas por posición del DataTable que devuelve sp_DQS_ConsultaUsuario,
// tal como las leía Login3.aspx.vb (dt.Rows(0).Item(N)). Los índices 0-17, 23 y 24 están
// cruzados y confirmados contra Imperium.Domain.Login.ResultadoUsuario (Recobros, mismo
// SP) — coinciden exactamente. Login3 además usa 18-22, 25 y 26, que Recobros nunca leía;
// se agregan aquí con el mismo criterio "LeerSeguro" por si el SP no siempre las trae.
//
// Nota sobre el índice 25 en el legado: Login3.aspx.vb lo asigna a la vez a
// Session("marcap2") y a Session("regional") (dos variables distintas leyendo la misma
// columna) — es casi con certeza un error de copiar/pegar del legado, no una decisión
// real. Se documenta como MarcaP2Regional (un solo campo) en vez de duplicar el bug con
// dos propiedades idénticas.
public class ResultadoUsuario
{
    public required string Cia { get; init; }
    public required string Rol { get; init; }
    public required string Nombre { get; init; }
    public required string IdEmp { get; init; }
    public required string RolParaModulos { get; init; }
    public required string UsuarioApp { get; init; }
    public string Logo { get; init; } = "";
    public string CodigoEps { get; init; } = "";
    public string Fecha { get; init; } = "";
    public string LogoEmpresa { get; init; } = "";
    public string TextoCarta { get; init; } = "";
    public string TextoNotificacion { get; init; } = "";
    public string NombreCliente { get; init; } = "";
    public string RutaPdf { get; init; } = "";
    public string Firma { get; init; } = "";
    public string NitCliente { get; init; } = "";
    public string DireccionCliente { get; init; } = "";
    public string TelefonoCliente { get; init; } = "";
    public string Token { get; init; } = "";
    public string EmailCliente { get; init; } = "";
    public string ProCli { get; init; } = "";
    public string Cedula { get; init; } = "";
    public string MarcaP { get; init; } = "";
    public string? ClaveTemporalReferencia { get; init; }
    public string? FechaVencimientoClave { get; init; }
    public string MarcaP2Regional { get; init; } = "";
    public string EmailUsuario { get; init; } = "";

    public static ResultadoUsuario DesdeFila(DataRow fila) => new()
    {
        Cia = Convert.ToString(fila[0]) ?? "",
        Rol = Convert.ToString(fila[1]) ?? "",
        Nombre = Convert.ToString(fila[2]) ?? "",
        IdEmp = Convert.ToString(fila[3]) ?? "",
        RolParaModulos = Convert.ToString(fila[4]) ?? "",
        UsuarioApp = Convert.ToString(fila[5]) ?? "",
        Logo = LeerSeguro(fila, 6),
        CodigoEps = LeerSeguro(fila, 7),
        Fecha = LeerSeguro(fila, 8),
        LogoEmpresa = LeerSeguro(fila, 9),
        TextoCarta = LeerSeguro(fila, 10),
        TextoNotificacion = LeerSeguro(fila, 11),
        NombreCliente = LeerSeguro(fila, 12),
        RutaPdf = LeerSeguro(fila, 13),
        Firma = LeerSeguro(fila, 14),
        NitCliente = LeerSeguro(fila, 15),
        DireccionCliente = LeerSeguro(fila, 16),
        TelefonoCliente = LeerSeguro(fila, 17),
        Token = LeerSeguro(fila, 18),
        EmailCliente = LeerSeguro(fila, 19),
        ProCli = LeerSeguro(fila, 20),
        Cedula = LeerSeguro(fila, 21),
        MarcaP = LeerSeguro(fila, 22),
        ClaveTemporalReferencia = LeerSeguro(fila, 23, permitirNulo: true),
        FechaVencimientoClave = LeerSeguro(fila, 24, permitirNulo: true),
        MarcaP2Regional = LeerSeguro(fila, 25),
        EmailUsuario = LeerSeguro(fila, 26),
    };

    private static string? LeerSeguro(DataRow fila, int indice, bool permitirNulo = false)
    {
        if (indice >= fila.Table.Columns.Count || fila.IsNull(indice))
        {
            return permitirNulo ? null : "";
        }

        return Convert.ToString(fila[indice]);
    }
}
