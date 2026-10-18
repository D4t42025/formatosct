using System.Data;
using Contingencia.Infrastructure.Correo;
using Contingencia.Infrastructure.Formatos;
using Contingencia.Infrastructure.Mipres;

namespace Contingencia.Application.Formatos;

public class FormatosService(
    IFormatosRepository repository,
    IMipresPrescripcionValidator mipresValidator,
    IEmailSender emailSender) : IFormatosService
{
    // Mismo conjunto de extensiones permitidas que JuntasFormatoCntC.aspx.vb — el UI del
    // legado anuncia solo ".pdf,.tiff,.png" (atributo "accept" del FileUpload) pero el
    // code-behind en realidad acepta más; se replica la lista real, no la anunciada.
    private static readonly string[] ExtensionesPermitidas =
        [".png", ".jpg", ".jpeg", ".tiff", ".docx", ".doc", ".xlsx", ".pdf"];

    public async Task<IReadOnlyList<PrestadorHabilitacion>> BuscarPrestadoresAsync(string prefijo, string? nit = null)
    {
        // Sin NIT (EPS: catálogo nacional de ~400 mil prestadores) no se busca con el texto vacío;
        // con NIT (IPS: pocas sedes propias) sí, y se listan todas.
        if (string.IsNullOrWhiteSpace(prefijo) && nit is null) return [];

        var dt = await repository.BuscarPrestadorAsync(prefijo ?? "", 100, nit);
        return dt.AsEnumerable()
            .Select(f => new PrestadorHabilitacion(
                Convert.ToString(f["Codigo"]) ?? "",
                Convert.ToString(f["Nombre"]) ?? ""))
            .ToList();
    }

    public async Task<DetallePrestador?> ObtenerDetallePrestadorAsync(string codigo, string? nit = null)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;

        var dt = await repository.ObtenerDetalleAsync(codigo, nit);
        if (dt.Rows.Count == 0) return null;

        var f = dt.Rows[0];
        return new DetallePrestador(
            Convert.ToString(f["Codigo"]) ?? "",
            Convert.ToString(f["Nombre"]) ?? "",
            NuloSiVacio(f["Nit"]),
            NuloSiVacio(f["Departamento"]),
            NuloSiVacio(f["Municipio"]),
            NuloSiVacio(f["Clase"]),
            string.Equals(Convert.ToString(f["Habilitado"]), "SI", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NuloSiVacio(object valor)
    {
        var texto = Convert.ToString(valor);
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }

    public async Task<ResultadoCargaFormato> ValidarYRegistrarAsync(
        string cia, string usuario, string tipoDocumento, string numeroDocumento,
        string prestador, string observaciones, string extensionArchivo, bool hayArchivo,
        string email, string fechaFormato)
    {
        // Réplica fiel del orden exacto de validaciones en btngenerar_ServerClick.
        if (!hayArchivo)
        {
            return new ResultadoCargaFormato(false, "Debe ingresar un documento", null);
        }

        if (!ExtensionesPermitidas.Contains(extensionArchivo.ToLowerInvariant()))
        {
            return new ResultadoCargaFormato(false, "Archivo no permitido", null);
        }

        if (tipoDocumento is "" or "Ninguno")
        {
            return new ResultadoCargaFormato(false, "Debe ingresar el tipo de documento", null);
        }

        if (string.IsNullOrEmpty(numeroDocumento))
        {
            return new ResultadoCargaFormato(false, "Debe ingresar no documento", null);
        }

        if (string.IsNullOrEmpty(prestador))
        {
            return new ResultadoCargaFormato(false, "Debe ingresar el prestador", null);
        }

        if (string.IsNullOrEmpty(email))
        {
            return new ResultadoCargaFormato(false, "Debe ingresar el email", null);
        }

        if (string.IsNullOrEmpty(fechaFormato))
        {
            return new ResultadoCargaFormato(false, "Debe ingresar la fecha", null);
        }

        var radicado = await repository.InsertarAsync(
            cia, tipoDocumento, numeroDocumento, prestador, observaciones,
            extensionArchivo.ToLowerInvariant(), email, fechaFormato, usuario);

        return new ResultadoCargaFormato(true, null, radicado);
    }

    public async Task<ResultadoConsultaFormatos> ConsultarAsync(
        string cia, string usuario, string tipoDocumento, string numeroDocumento,
        string prestador, string radicado, string fechaInicio, string fechaFin, string estado)
    {
        // Réplica exacta de las dos validaciones de btnconsultar_ServerClick /
        // imgexporta_Click (idénticas en ambos). "numeroDocumento" corresponde al control
        // legado txtusuario.Value — nombre engañoso, no tiene relación con el usuario de
        // sesión (ese va aparte, fijo, como @USUARIO del SP). "Fecha Fin" y "Estado" NUNCA
        // contaron para esta validación, ni en el legado ni acá — son solo un filtro
        // adicional, no un criterio obligatorio.
        if (numeroDocumento == "" && prestador == "" && radicado == "" && fechaInicio == "")
        {
            return new ResultadoConsultaFormatos(false, "Ingrese mínimo un criterio de búsqueda", []);
        }

        if ((numeroDocumento != "" && tipoDocumento == "Ninguno") ||
            (numeroDocumento == "" && tipoDocumento != "Ninguno"))
        {
            return new ResultadoConsultaFormatos(false, "Debe ingresar no documento y tipo de documento", []);
        }

        var tipo = tipoDocumento == "Ninguno" ? "%" : tipoDocumento;
        var id = numeroDocumento == "" ? "%" : numeroDocumento;
        var nit = prestador == "" ? "%" : prestador;
        var rad = radicado == "" ? "%" : radicado;

        // El SP solo acepta UN valor de fecha (LIKE exacto contra un día, no un rango) — para
        // poder filtrar por Fecha Inicio Y Fecha Fin como un rango real, se le pide todo
        // ("%") y el rango completo (incluida Fecha Inicio) se aplica en memoria abajo.
        var dt = await repository.ConsultarAsync(cia, tipo, id, nit, rad, "%", usuario);
        var formatos = dt.AsEnumerable()
            .Select(f => new FormatoConsultado(
                Convert.ToString(f["CIA"]) ?? "",
                Convert.ToString(f["TIPOID"]) ?? "",
                Convert.ToString(f["NUMID"]) ?? "",
                Convert.ToString(f["NIT"]) ?? "",
                Convert.ToString(f["OBJ"]) ?? "",
                Convert.ToString(f["RADICADO"]) ?? "",
                Convert.ToString(f["RUTA"]) ?? "",
                Convert.ToString(f["FECHA"]) ?? "",
                Convert.ToString(f["FECHAFORMATO"]) ?? "",
                Convert.ToString(f["NOPRESCRIPCION"]) ?? "",
                Convert.ToString(f["OBSERVACIONES"]) ?? "",
                Convert.ToString(f["ESTADO"]) ?? ""))
            .AsEnumerable();

        if (fechaInicio != "" && DateTime.TryParse(fechaInicio, out var desde))
        {
            formatos = formatos.Where(f => TryParseFecha(f.Fecha, out var d) && d.Date >= desde.Date);
        }

        if (fechaFin != "" && DateTime.TryParse(fechaFin, out var hasta))
        {
            formatos = formatos.Where(f => TryParseFecha(f.Fecha, out var d) && d.Date <= hasta.Date);
        }

        if (estado != "" && estado != "Todo")
        {
            formatos = formatos.Where(f => f.Estado == estado);
        }

        return new ResultadoConsultaFormatos(true, null, formatos.ToList());
    }

    private static bool TryParseFecha(string fecha, out DateTime resultado) =>
        DateTime.TryParse(fecha, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out resultado);

    // Réplica de btnguardar_ServerClick en JuntasFormatoCntCO2.aspx.vb, mismo orden de
    // validaciones y mismos mensajes exactos.
    public async Task<ResultadoGestionFormato> GestionarAsync(
        string cia, string usuario, string radicado, string mipres, string observaciones, string estado)
    {
        if (mipres == "")
        {
            return new ResultadoGestionFormato(false, "Debe ingresar el mipres");
        }

        if (estado is "" or "Seleccionar")
        {
            return new ResultadoGestionFormato(false, "Debe ingresar el estado");
        }

        if (estado == "Gestionado")
        {
            bool prescripcionValida;
            try
            {
                prescripcionValida = await mipresValidator.EsValidaAsync(cia, mipres);
            }
            catch
            {
                // A diferencia del legado (sin Try/Catch alrededor de esta llamada — un
                // fallo de red tumbaba la página WebForms entera con un error amarillo), acá
                // se atrapa para no tumbar el circuito de Blazor Server del usuario.
                return new ResultadoGestionFormato(false,
                    "No fue posible validar la prescripción contra el sistema MIPRES. Intente nuevamente.");
            }

            if (!prescripcionValida)
            {
                return new ResultadoGestionFormato(false, "No prescripción no valida");
            }
        }

        await repository.ActualizarGestionAsync(cia, mipres, observaciones, estado, usuario, radicado);

        // Réplica de crearcorreo() en JuntasFormatoCntCO2.aspx.vb: se envía aunque no sea
        // crítico para el resultado ya confirmado (el UPDATE ya se aplicó); si el SMTP falla,
        // no se revierte el guardado — mismo criterio que el flujo de recuperar clave.
        try
        {
            var email = await repository.ConsultarEmailAsync(cia, radicado);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var cuerpo = PlantillaConfirmacionGestion.Generar(radicado, estado, mipres);
                await emailSender.EnviarAsync(email, "Respuesta de formato de contingencia", cuerpo);
            }
        }
        catch
        {
            // Intencional, igual que el legado (Catch vacío en crearcorreo()).
        }

        return new ResultadoGestionFormato(true, "Confirmación correcta");
    }

    public async Task<ResultadoGestionFormato> EliminarAsync(string cia, string radicado)
    {
        var eliminado = await repository.EliminarAsync(cia, radicado);
        return eliminado
            ? new ResultadoGestionFormato(true, "Formato eliminado.")
            : new ResultadoGestionFormato(false, "No se encontró el formato para eliminar.");
    }
}
