using System.Data;
using System.Security.Cryptography;
using Contingencia.Infrastructure.Profesionales;

namespace Contingencia.Application.Profesionales;

public class ProfesionalesService(IProfesionalesRepository repository) : IProfesionalesService
{
    public async Task<List<ProfesionalMip>> ConsultarAsync(string cia, string? busqueda, string? profesion, string? sede, string? estado)
    {
        var dt = await repository.ConsultarAsync(cia, busqueda, profesion, sede, estado);
        return dt.AsEnumerable().Select(Mapear).ToList();
    }

    public async Task<ProfesionalMip?> ObtenerAsync(string cia, int idProfesional)
    {
        var dt = await repository.ObtenerAsync(cia, idProfesional);
        return dt.Rows.Count == 0 ? null : Mapear(dt.Rows[0]);
    }

    public async Task<ResultadoGuardarProfesional> GuardarAsync(
        string cia, int? idProfesional, SolicitudProfesional s, string usuarioSesion, string? idEmpresa)
    {
        // Se genera siempre, aunque solo se use si el procedimiento decide crear un usuario
        // nuevo — igual que GenerarClave() en el legado, llamado incondicionalmente en
        // btnGuardar_Click. Si el profesional ya tenía usuario, el procedimiento la ignora.
        var clave = GenerarClave();

        var dt = idProfesional is > 0
            ? await repository.ActualizarAsync(
                cia, idProfesional.Value, s.TipoDoc, s.NoIdentificacion, s.Apellidos, s.Nombres,
                s.Profesion, s.Sede, s.Servicio, s.Correo, s.Celular, s.FActivacion, s.Firma,
                usuarioSesion, idEmpresa, clave)
            : await repository.InsertarAsync(
                cia, s.TipoDoc, s.NoIdentificacion, s.Apellidos, s.Nombres,
                s.Profesion, s.Sede, s.Servicio, s.Correo, s.Celular, s.FActivacion, s.Firma,
                usuarioSesion, idEmpresa, clave);

        if (dt.Rows.Count == 0)
        {
            return new ResultadoGuardarProfesional(false, idProfesional ?? 0, "Ocurrió un error guardando el profesional. Intente de nuevo.", "", "", false);
        }

        var fila = dt.Rows[0];
        return new ResultadoGuardarProfesional(
            Convert.ToString(fila["RESULTADO"]) == "1",
            Convert.ToInt32(fila["IDPROFESIONAL"]),
            Convert.ToString(fila["MENSAJE"]) ?? "",
            Convert.ToString(fila["USUARIOAPP"]) ?? "",
            Convert.ToString(fila["CLAVEAPP"]) ?? "",
            Convert.ToString(fila["USUARIOCREADO"]) is "1" or "True" or "true");
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(string cia, int idProfesional, bool activar, string usuarioSesion)
    {
        var dt = await repository.CambiarEstadoAsync(cia, idProfesional, activar, usuarioSesion);
        if (dt.Rows.Count == 0) return new ResultadoOperacion(false, "Ocurrió un error cambiando el estado. Intente de nuevo.");

        var fila = dt.Rows[0];
        return new ResultadoOperacion(Convert.ToString(fila["RESULTADO"]) == "1", Convert.ToString(fila["MENSAJE"]) ?? "");
    }

    public async Task<ResumenProfesionales> ObtenerResumenAsync(string cia)
    {
        var dt = await repository.ResumenAsync(cia);
        if (dt.Rows.Count == 0) return new ResumenProfesionales(0, 0, 0, 0);

        var fila = dt.Rows[0];
        return new ResumenProfesionales(
            EnteroOCero(fila["TOTAL"]), EnteroOCero(fila["ACTIVOS"]), EnteroOCero(fila["INACTIVOS"]), EnteroOCero(fila["SEDES"]));
    }

    public async Task<(List<string> Profesiones, List<string> Sedes)> ObtenerListasAsync(string cia)
    {
        var dt = await repository.ListasAsync(cia);
        var filas = dt.AsEnumerable().ToList();

        var profesiones = filas.Where(f => Convert.ToString(f["TIPO"]) == "PROFESION")
            .Select(f => Convert.ToString(f["VALOR"]) ?? "").Where(v => v.Length > 0).ToList();
        var sedes = filas.Where(f => Convert.ToString(f["TIPO"]) == "SEDE")
            .Select(f => Convert.ToString(f["VALOR"]) ?? "").Where(v => v.Length > 0).ToList();

        return (profesiones, sedes);
    }

    private static ProfesionalMip Mapear(DataRow f) => new(
        Convert.ToInt32(f["IDPROFESIONAL"]),
        Convert.ToString(f["CIA"]) ?? "",
        Convert.ToString(f["TIPODOC"]) ?? "",
        Convert.ToString(f["NoIDENTIFICACION"]) ?? "",
        Convert.ToString(f["NOMBRECOMPLETO"]) ?? "",
        Convert.ToString(f["APELLIDOS"]) ?? "",
        Convert.ToString(f["NOMBRES"]) ?? "",
        Convert.ToString(f["PROFESION"]) ?? "",
        Convert.ToString(f["SEDE"]) ?? "",
        Convert.ToString(f["SERVICIO"]) ?? "",
        Convert.ToString(f["CORREOELECTRONICO"]) ?? "",
        Convert.ToString(f["CELULAR"]) ?? "",
        Convert.ToString(f["FACTIVACION"]) ?? "",
        Convert.ToString(f["FIRMA"]) ?? "",
        Convert.ToString(f["ESTADO"]) is "1" or "True" or "true",
        Convert.ToString(f["ESTADODESC"]) ?? "",
        f["FECHACREACION"] is DateTime fc ? fc : null,
        Convert.ToString(f["USUARIOCREACION"]) ?? "",
        f["FECHAACTUALIZACION"] is DateTime fa ? fa : null,
        Convert.ToString(f["USUARIOACTUALIZACION"]) ?? "",
        // sp_DQS_MIP_ProfesionalesObtener no trae estas dos columnas (solo Consultar) — el
        // formulario de edición no las necesita, así que no vale la pena traerlas ahí también.
        f.Table.Columns.Contains("FECHAINACTIVACION") && f["FECHAINACTIVACION"] is DateTime fi ? fi : null,
        f.Table.Columns.Contains("USUARIOINACTIVACION") ? Convert.ToString(f["USUARIOINACTIVACION"]) ?? "" : "",
        Convert.ToString(f["USUARIOAPP"]) ?? "");

    private static int EnteroOCero(object valor) => valor is DBNull ? 0 : Convert.ToInt32(valor);

    // Réplica exacta de GenerarClave() en MIP_PROFESIONALES.aspx.vb: un carácter de cada
    // grupo obligatorio, relleno hasta 12, barajado con Fisher-Yates para que los 4
    // obligatorios no queden siempre en las mismas posiciones.
    private static string GenerarClave()
    {
        const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minusculas = "abcdefghijkmnopqrstuvwxyz";
        const string numeros = "23456789";
        const string simbolos = "*$-+?_&=!%";
        var todos = mayusculas + minusculas + numeros + simbolos;

        var clave = new List<char>
        {
            CaracterAlAzar(mayusculas),
            CaracterAlAzar(minusculas),
            CaracterAlAzar(numeros),
            CaracterAlAzar(simbolos),
        };

        while (clave.Count < 12)
        {
            clave.Add(CaracterAlAzar(todos));
        }

        for (var i = clave.Count - 1; i >= 1; i--)
        {
            var j = EnteroAlAzar(i + 1);
            (clave[i], clave[j]) = (clave[j], clave[i]);
        }

        return new string(clave.ToArray());
    }

    private static char CaracterAlAzar(string alfabeto) => alfabeto[EnteroAlAzar(alfabeto.Length)];

    // Entero en [0, tope) sin sesgo: se descartan los valores del último tramo incompleto.
    private static int EnteroAlAzar(int tope)
    {
        if (tope <= 1) return 0;

        Span<byte> buffer = stackalloc byte[4];
        var limite = uint.MaxValue - (uint.MaxValue % (uint)tope);

        while (true)
        {
            RandomNumberGenerator.Fill(buffer);
            var valor = BitConverter.ToUInt32(buffer);
            if (valor < limite) return (int)(valor % (uint)tope);
        }
    }
}
