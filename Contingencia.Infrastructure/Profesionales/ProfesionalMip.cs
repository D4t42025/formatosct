namespace Contingencia.Infrastructure.Profesionales;

// Refleja el SELECT de sp_DQS_MIP_ProfesionalesConsultar/Obtener (2026-09-25, ver
// BASE_DE_DATOS.md sección "Página 10"). Firma es la ruta relativa del archivo, no la
// imagen — para el PDF del acta esa ruta pasa por IFirmaImagenService, no por acá.
public record ProfesionalMip(
    int IdProfesional,
    string Cia,
    string TipoDoc,
    string NoIdentificacion,
    string NombreCompleto,
    string Apellidos,
    string Nombres,
    string Profesion,
    string Sede,
    string Servicio,
    string CorreoElectronico,
    string Celular,
    string FActivacion,
    string Firma,
    bool Estado,
    string EstadoDesc,
    DateTime? FechaCreacion,
    string UsuarioCreacion,
    DateTime? FechaActualizacion,
    string UsuarioActualizacion,
    DateTime? FechaInactivacion,
    string UsuarioInactivacion,
    string UsuarioApp);

public record ResumenProfesionales(int Total, int Activos, int Inactivos, int Sedes);

// Datos de un profesional a guardar (alta o edición) — validados/normalizados por
// ProfesionalesService antes de llegar al repositorio.
public record SolicitudProfesional(
    string? TipoDoc,
    string NoIdentificacion,
    string? Apellidos,
    string? Nombres,
    string? Profesion,
    string? Sede,
    string? Servicio,
    string? Correo,
    string? Celular,
    string? FActivacion,
    string? Firma);

// Espejo de RESULTADO/IDPROFESIONAL/MENSAJE/USUARIOAPP/CLAVEAPP/USUARIOCREADO que devuelven
// sp_DQS_MIP_ProfesionalesInsertar/Actualizar.
public record ResultadoGuardarProfesional(
    bool Exito, int IdProfesional, string Mensaje, string UsuarioApp, string ClaveApp, bool UsuarioCreado);

public record ResultadoOperacion(bool Exito, string Mensaje);
