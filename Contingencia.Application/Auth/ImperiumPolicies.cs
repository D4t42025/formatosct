namespace Contingencia.Application.Auth;

public static class ImperiumPolicies
{
    // Exige el módulo "Junta Profesionales" (claim imperium:modulo, armado con sp_DQS_ConsultaModxRol a
    // partir del rol de la CIA). Una CIA que solo usa Formatos (rol 601 → módulo "Contingencia", p. ej.
    // la IPS 800006850) no lo tiene, así que /juntas, /profesionales y /api/juntas/* le quedan vedados
    // aunque escriba la URL a mano; antes bastaba con tener sesión.
    public const string ModuloJuntas = "ModuloJuntas";
    public const string NombreModuloJuntas = "Junta Profesionales";

    // Gestión de profesionales (/profesionales) sin Juntas: la habilita el módulo "Profesionales" (ID 494 en
    // DQS_REC_MODULOS, asignado por CIA al rol 601 — hoy solo el hospital 800006850) o, como siempre, el de Juntas.
    public const string ModuloProfesionales = "ModuloProfesionales";
    public const string NombreModuloProfesionales = "Profesionales";
}
