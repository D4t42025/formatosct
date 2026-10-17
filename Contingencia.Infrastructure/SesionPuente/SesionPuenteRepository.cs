using System.Data;
using Dapper;
using Contingencia.Domain.SesionPuente;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Contingencia.Infrastructure.SesionPuente;

// Misma tabla física que usa Recobros (dbo.ImperiumSesionPuente, base DQUALITY
// compartida) — ver Imperium.Infrastructure/Sql/001_CrearTablaPuenteSesion.sql. No es un
// SP legado, así que aquí sí conocemos los nombres de columna y usamos Dapper directo.
public class SesionPuenteRepository(IConfiguration configuration) : ISesionPuenteRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DQUALITY")
        ?? throw new InvalidOperationException("Falta configurar la cadena de conexión 'DQUALITY'.");

    public async Task<Guid> CrearAsync(DatosSesionPuente datos, DataTable modulos, TimeSpan vigencia)
    {
        var token = Guid.NewGuid();
        var modulosJson = JsonConvert.SerializeObject(modulos);
        const string sql = """
            INSERT INTO dbo.ImperiumSesionPuente
                (Token, Cia, Rol, Nombre, IdEmp, UsuApp, Logo, CodigoEps, Fecha, LogoEmpresa,
                 TextoCarta, TextoNotificacion, NombreCliente, RutaPdf, Firma, NitCliente,
                 DireccionCliente, TelefonoCliente, PasswordInicial, ModActIni, ModulosJson, Log,
                 IdRol, TokenMipres, CreadoEn, ExpiraEn)
            VALUES
                (@Token, @Cia, @Rol, @Nombre, @IdEmp, @UsuApp, @Logo, @CodigoEps, @Fecha, @LogoEmpresa,
                 @TextoCarta, @TextoNotificacion, @NombreCliente, @RutaPdf, @Firma, @NitCliente,
                 @DireccionCliente, @TelefonoCliente, @PasswordInicial, @ModActIni, @ModulosJson, @Log,
                 @IdRol, @TokenMipres, SYSUTCDATETIME(), @ExpiraEn)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            Token = token,
            datos.Cia,
            datos.Rol,
            datos.Nombre,
            datos.IdEmp,
            datos.UsuApp,
            datos.Logo,
            datos.CodigoEps,
            datos.Fecha,
            datos.LogoEmpresa,
            datos.TextoCarta,
            datos.TextoNotificacion,
            datos.NombreCliente,
            datos.RutaPdf,
            datos.Firma,
            datos.NitCliente,
            datos.DireccionCliente,
            datos.TelefonoCliente,
            datos.PasswordInicial,
            datos.ModActIni,
            ModulosJson = modulosJson,
            datos.Log,
            datos.IdRol,
            datos.TokenMipres,
            ExpiraEn = DateTime.UtcNow.Add(vigencia),
        });

        return token;
    }

    public async Task<(DatosSesionPuente Datos, IReadOnlyList<string> Modulos)?> ConsumirAsync(Guid token)
    {
        const string sqlSelect = """
            SELECT Cia, Rol, Nombre, IdEmp, UsuApp, Logo, CodigoEps, Fecha, LogoEmpresa,
                   TextoCarta, TextoNotificacion, NombreCliente, RutaPdf, Firma, NitCliente,
                   DireccionCliente, TelefonoCliente, PasswordInicial, ModActIni, ModulosJson, Log,
                   IdRol, TokenMipres
            FROM dbo.ImperiumSesionPuente
            WHERE Token = @Token AND ExpiraEn > SYSUTCDATETIME()
            """;
        const string sqlDelete = "DELETE FROM dbo.ImperiumSesionPuente WHERE Token = @Token";

        await using var connection = new SqlConnection(_connectionString);
        var fila = await connection.QueryFirstOrDefaultAsync<FilaPuente>(sqlSelect, new { Token = token });
        await connection.ExecuteAsync(sqlDelete, new { Token = token });

        if (fila is null)
        {
            return null;
        }

        var modulos = new List<string>();
        if (!string.IsNullOrWhiteSpace(fila.ModulosJson))
        {
            modulos = JArray.Parse(fila.ModulosJson)
                .Select(elemento => elemento["MODULO"]?.ToString() ?? "")
                .Where(nombre => nombre.Length > 0)
                .ToList();
        }

        var datos = new DatosSesionPuente
        {
            Cia = fila.Cia,
            Rol = fila.Rol,
            Nombre = fila.Nombre,
            IdEmp = fila.IdEmp,
            UsuApp = fila.UsuApp,
            Logo = fila.Logo ?? "",
            CodigoEps = fila.CodigoEps ?? "",
            Fecha = fila.Fecha ?? "",
            LogoEmpresa = fila.LogoEmpresa ?? "",
            TextoCarta = fila.TextoCarta ?? "",
            TextoNotificacion = fila.TextoNotificacion ?? "",
            NombreCliente = fila.NombreCliente ?? "",
            RutaPdf = fila.RutaPdf ?? "",
            Firma = fila.Firma ?? "",
            NitCliente = fila.NitCliente ?? "",
            DireccionCliente = fila.DireccionCliente ?? "",
            TelefonoCliente = fila.TelefonoCliente ?? "",
            PasswordInicial = fila.PasswordInicial,
            ModActIni = fila.ModActIni ?? "",
            Log = fila.Log ?? "",
            IdRol = fila.IdRol ?? "",
            TokenMipres = fila.TokenMipres ?? "",
        };

        return (datos, modulos);
    }

    private class FilaPuente
    {
        public required string Cia { get; init; }
        public required string Rol { get; init; }
        public required string Nombre { get; init; }
        public required string IdEmp { get; init; }
        public required string UsuApp { get; init; }
        public string? Logo { get; init; }
        public string? CodigoEps { get; init; }
        public string? Fecha { get; init; }
        public string? LogoEmpresa { get; init; }
        public string? TextoCarta { get; init; }
        public string? TextoNotificacion { get; init; }
        public string? NombreCliente { get; init; }
        public string? RutaPdf { get; init; }
        public string? Firma { get; init; }
        public string? NitCliente { get; init; }
        public string? DireccionCliente { get; init; }
        public string? TelefonoCliente { get; init; }
        public string? PasswordInicial { get; init; }
        public string? ModActIni { get; init; }
        public string? ModulosJson { get; init; }
        public string? Log { get; init; }
        public string? IdRol { get; init; }
        public string? TokenMipres { get; init; }
    }
}
