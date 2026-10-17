using Contingencia.Domain.Menu;

namespace Contingencia.Application.Menu;

// Equivalente a Opciones.aspx.vb del proyecto legado. El legado deja UN bloque de tarjetas activo y
// el resto comentado (se cambia editando el código según el cliente); aquí cada bloque es un perfil
// y el activo se elige con la clave de configuración "Menu:Perfil" (por defecto "SANITASMIPRES"; el legado
// tiene activo "IPS" desde 2026-10-13 pero estas apps se publican con SANITASMIPRES). Perfiles: IPS, INCAPACIDADES, PLUS, SANITASMIPRES,
// TUTELAS, INFORMES_GF y FORMATOS_JUNTAS. FORMATOS_JUNTAS es el de la base de producción nueva (Formatos + Juntas, multi-CIA):
// solo trae tarjetas que funcionan en esta app, porque los demás perfiles apuntan a pantallas del sistema anterior.
//
// Recobros y Contingencia comparten esta pantalla de entrada. "/login" es el Login3.aspx migrado en
// esta app: en el perfil IPS la tarjeta "Mipres" va ahí (rol 601, Formato de Contingencia; rol 60 →
// Juntas Profesionales); en SANITASMIPRES lo hacen "Formato Integralidad" y "Gestor Juntas" (la
// tarjeta "Mipres" conserva Login3.aspx como en el legado). El resto de tarjetas apunta, igual que
// el legado, a sus .aspx sin migrar.
public class MenuOpcionesService(string? perfil = null) : IMenuOpcionesService
{
    private const string BaseImg = "OpcionesN/assetsN/images/";
    private const string PerfilPorDefecto = "SANITASMIPRES";

    private static AppCard Tarjeta(string nombre, string descripcion, string href, string icono) =>
        new() { Nombre = nombre, Descripcion = descripcion, Href = href, Icono = BaseImg + icono };

    private static readonly AppCard HelpSoporte = Tarjeta("Help Soporte", "Gestión centralizada de incidencias.<br>Mejora la satisfacción del usuario.", "https://help.dqualitysolutions.com.co/Login.aspx", "soporte-tecnico.png");
    private static readonly AppCard HistoriaClinica = Tarjeta("Historia Clinica", "Registro cronológico de datos clínicos y administrativos del paciente.", "Login4.aspx", "carpeta-medica.png");
    private static readonly AppCard CuentasMedicas = Tarjeta("Cuentas Medicas", "Revisión, auditoría y radicación de facturación por servicios prestados en salud.", "Login2.aspx", "medico.png");
    private static readonly AppCard Autorizaciones = Tarjeta("Autorizaciones", "Validación, trámite y radicación de servicios médicos sujetos a autorización por la EPS.", "Login7.aspx", "autorizacion.png");
    private static readonly AppCard Recobros = Tarjeta("Recobros", "Preparación, tramite, liquidación, radicación y cartera ante los entes estables.", "Login.aspx", "pago.png");

    private static readonly Dictionary<string, AppCard[]> Perfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        // IPS — bloque activo en el legado (2026-10-13); no es el perfil por defecto de esta app.
        ["IPS"] =
        [
            Tarjeta("Mipres", "Prescripción de tecnologías en salud no financiadas por Unidad de Pago por Capitación.", "/login", "prescripcion-medica.png"),
            HelpSoporte,
            Tarjeta("Laboratorios", "Procesamiento, gestión, control y seguimiento en entrega de medicamentos.", "Loginlab.aspx", "microscopio.png"),
            Tarjeta("RIPS", "Registro y validación de información individual sobre servicios de salud prestados.", "Loginlab.aspx", "Rips.png"),
            Tarjeta("Facturador", "Programación y auditoría de facturas electrónicas por servicios de salud.", "Loginlab.aspx", "factura.png"),
            HistoriaClinica,
            Tarjeta("Inventarios", "Control de existencias, entradas, salidas y disponibilidad de insumos.", "Login9.aspx", "inventario.png"),
        ],
        ["INCAPACIDADES"] =
        [
            Tarjeta("Incapacidades", "Gestión de incapacidades médicas.<br>Generación de reportes.", "Login10.aspx", "incapacidad.png"),
            HelpSoporte,
        ],
        ["PLUS"] =
        [
            Tarjeta("Mipres", "Prescripción de tecnologías en salud no financiadas por Unidad de Pago por Capitación.", "Login3.aspx", "prescripcion-medica.png"),
            Tarjeta("Recobros", "Preparación, trámite, liquidación y radicación de cobros ante entes estatales.", "Login.aspx", "pago.png"),
            HelpSoporte,
            Tarjeta("Gestor Integral", "Coordinación, seguimiento y gestión de servicios en salud dentro de la red asistencial.", "Login10.aspx", "financiero.png"),
            Tarjeta("Gestor Juntas", "Trámite de solicitudes ante juntas médicas por servicios no financiados.", "/login", "gestor-de-la-comunidad.png"),
            HistoriaClinica,
            CuentasMedicas,
            Autorizaciones,
        ],
        ["SANITASMIPRES"] =
        [
            Recobros,
            Tarjeta("Tutelas", "Radicación y control de tutelas.<br>Alertas de vencimientos procesales.", "https://imperiumgitsanitas.net/LoginTUT.aspx", "juez.png"),
            Tarjeta("Mipres", "Prescripción de tecnologías en salud no financiadas por Unidad de Pago por Capitación.", "Login3.aspx", "prescripcion-medica.png"),
            HelpSoporte,
            Tarjeta("Formato Integralidad", "Registro del cumplimiento de acciones integrales en salud según RIAS.", "/login", "documento.png"),
            Tarjeta("Gestor Juntas", "Trámite y gestión de solicitudes ante juntas médicas por servicios no financiados.", "/login", "gestor-de-la-comunidad.png"),
            CuentasMedicas,
            Autorizaciones,
        ],
        ["TUTELAS"] =
        [
            HelpSoporte,
            Tarjeta("Tutelas", "Radicación y control de tutelas.<br>Alertas de vencimientos procesales.", "LoginTUT.aspx", "juez.png"),
            Tarjeta("Mipres", "Prescripción de tecnologías en salud no financiadas por Unidad de Pago por Capitación.", "https://imperiumsanitas.net.co/Login3.aspx", "prescripcion-medica.png"),
        ],
        // Despliegue de producción (Formatos + Juntas, multi-CIA): solo tarjetas que funcionan en esta app. Los demás perfiles apuntan a
        // pantallas del sistema anterior (Login.aspx, Login2.aspx, Login7.aspx…) o a sitios de Sanitas que no existen en esta instalación.
        // Ambas tarjetas van al mismo login (/login): el rol del usuario decide a dónde entra (601 → Formatos, 60 → Juntas).
        ["FORMATOS_JUNTAS"] =
        [
            Tarjeta("Formatos de Contingencia", "Radicación y consulta de formatos de contingencia MIPRES y de nuevas contingencias con datos estructurados.", "/login", "documento.png"),
            Tarjeta("Gestor Juntas", "Trámite y gestión de solicitudes ante juntas médicas por servicios no financiados.", "/login", "gestor-de-la-comunidad.png"),
            HelpSoporte,
        ],
        ["INFORMES_GF"] =
        [
            HelpSoporte,
            Tarjeta("Gestor Farmaceutico", "Coordinación, seguimiento y gestión de servicios en salud dentro de la red asistencial.", "https://gf.imperiumeps.net/Login12.aspx", "gestor-de-la-comunidad.png"),
            Tarjeta("Informes Gestor", "Informes operativos para control seguimiento y auditoria al proceso de gestión de servicios en salud.", "Login12.aspx", "medico.png"),
        ],
    };

    public IReadOnlyList<AppCard> ObtenerTarjetas()
    {
        var clave = string.IsNullOrWhiteSpace(perfil) ? PerfilPorDefecto : perfil.Trim();
        return Perfiles.TryGetValue(clave, out var tarjetas) ? tarjetas : Perfiles[PerfilPorDefecto];
    }
}
