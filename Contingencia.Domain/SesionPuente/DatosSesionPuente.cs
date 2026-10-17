namespace Contingencia.Domain.SesionPuente;

// Todo lo que Login3.aspx.vb guardaba en Session(...) tras un login exitoso.
// Se serializa en la MISMA tabla puente que usa Recobros (dbo.ImperiumSesionPuente,
// base DQUALITY compartida) — los tokens son GUID únicos de un solo uso, así que no hay
// riesgo de colisión entre las dos apps escribiendo/leyendo la misma tabla.
//
// Solo se llevan los campos que YA existen como columna en esa tabla (ver
// Imperium.Infrastructure/Sql/001_CrearTablaPuenteSesion.sql). Login3 lee más columnas
// del SP (Token, EmailCliente, ProCli, Cedula, MarcaP, MarcaP2/Regional, EmailUsuario —
// ver Contingencia.Domain.Login.ResultadoUsuario) que las que caben aquí hoy; si la
// página destino (Inicio / JuntasFormatoCnt) termina necesitando alguna, hay que agregar
// la columna a dbo.ImperiumSesionPuente con un ALTER TABLE aditivo (no rompe a Recobros).
public record DatosSesionPuente
{
    public required string Cia { get; init; }
    public required string Rol { get; init; }
    public required string Nombre { get; init; }
    public required string IdEmp { get; init; }
    public required string UsuApp { get; init; }
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
    public string? PasswordInicial { get; init; }
    public string ModActIni { get; init; } = "";
    public required string Log { get; init; }

    // Columna 4 del SP de login (Session("idrol") en Login3.aspx.vb) — DISTINTA de "Rol"
    // (columna 1, arriba). Login3 usa idrol, no Rol, para decidir destino/branching por rol
    // (idrol="601", etc.) — se agrega porque el modal de "Gestionar formato" necesita saber
    // el idrol real del usuario para decidir si puede gestionar (roles 27/29/30), y hasta
    // ahora esta tabla solo guardaba "Rol". Columna aditiva en dbo.ImperiumSesionPuente
    // (ver Imperium.Infrastructure/Sql/005_AgregaIdRolPuenteSesion.sql), no rompe Recobros.
    public string IdRol { get; init; } = "";

    // Columna 18 del SP de login (Session("token") en Login3.aspx.vb) — usada por
    // JuntaProfesional.aspx.vb para decidir, en cada llamada a la API de SISPRO, si pedir
    // el token "APIPRESCRIPCION2" (cuando vale "IPS") o "APIPRESCRIPCION" (cualquier otro
    // caso, incluido vacío) contra DQS_MIP_TOKEN. Columna aditiva en
    // dbo.ImperiumSesionPuente (ver Imperium.Infrastructure/Sql/006_AgregaTokenMipresPuenteSesion.sql),
    // no rompe Recobros. Se llama "TokenMipres" y no "Token" porque esa tabla ya tiene una
    // columna PK con ese nombre (el GUID de la sesión puente).
    public string TokenMipres { get; init; } = "";
}
