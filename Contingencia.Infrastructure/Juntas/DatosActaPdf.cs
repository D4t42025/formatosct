using Contingencia.Infrastructure.Mipres;

namespace Contingencia.Infrastructure.Juntas;

// ImagenFirma ya viene descargada (bytes crudos del archivo, sin recortar ni escalar) —
// JuntasService la trae por HTTP desde el legado (ver IFirmaImagenService) antes de armar
// este registro; ActaPdfBuilder solo la recorta/escala/inserta, sin acceso a red. Es null
// si el profesional no tiene firma o si no se pudo descargar (una firma que falta no puede
// impedir que se imprima el acta — mismo criterio que FirmaActa.vb en el legado).
public record MiembroJunta(string NoIdentificacion, string ApellidosYNombres, string Profesion, byte[]? ImagenFirma);

// Todo lo que gvconsulta_OnRowCommand (CommandName="PDF") necesita para armar el acta —
// ya resuelto por JuntasService (cruces contra DQS_MIP_CRUZAR incluidos), para que
// ActaPdfBuilder sea una capa de solo maquetación, sin lógica de negocio ni acceso a datos.
public record DatosActaPdf(
    string NumeroActa,
    string FechaActa,
    string Modalidad,
    string FechaExpedicion,
    string JustificacionTecnica,
    string Decision,
    // "Departamento" y "Municipio" muestran literalmente el mismo valor en el acta — el
    // legado (gvconsulta_OnRowCommand) nunca tuvo un dato de departamento separado, ambas
    // celdas leen la misma variable "municipio" (copy/paste del legado, replicado tal cual).
    string Municipio,
    string CodigoHabilitacion,
    string NombrePrestador,
    string Direccion,
    string Telefono,
    string DocumentoPaciente,
    string PrimerApellido,
    string SegundoApellido,
    string PrimerNombre,
    string SegundoNombre,
    string HistoriaClinica,
    string DiagnosticoPrincipal,
    string AmbitoAtencion,
    IReadOnlyList<MipresMedicamento> Medicamentos,
    IReadOnlyList<MipresProcedimiento> Procedimientos,
    IReadOnlyList<MipresDispositivo> Dispositivos,
    IReadOnlyList<MipresProductoNutricional> ProductosNutricionales,
    IReadOnlyList<MipresServicioComplementario> ServiciosComplementarios,
    IReadOnlyList<MiembroJunta> Profesionales);
