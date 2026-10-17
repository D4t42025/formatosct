using System.Data;

namespace Contingencia.Infrastructure.Formatos;

public interface IFormatosRepository
{
    // sp_MIP_InsertaFormatoCnt genera el radicado (AAAAMMDD + consecutivo por COUNT(*) de
    // DQS_MIP_FORMATOS) y devuelve una sola fila/columna con ese radicado — mismo objeto de
    // base de datos que ya usa JuntasFormatoCntC.aspx.vb (objbll.insertafotmatocontigencia).
    Task<string> InsertarAsync(string cia, string tipoId, string numId, string nit, string obj,
        string extension, string email, string fechaFormato, string usuario);

    // nit != null restringe a los prestadores con ese Extra_II_NroIDPrestador (IPS: solo sus sedes).
    Task<DataTable> BuscarPrestadorAsync(string prefijo, int cantidad, string? nit = null);

    // Detalle puntual de un prestador ya elegido (wizard "Nueva Contingencia", paso 2) — solo
    // las columnas reales de DQS_MIP_COD_HAB (no tiene dirección/teléfono/correo, confirmado
    // contra las 199.423 filas de DQUALITYREC).
    Task<DataTable> ObtenerDetalleAsync(string codigo, string? nit = null);

    // sp_MIP_ConsultaFormatosCNT tiene una rama de comportamiento distinta por rol: para
    // rol 601 (el único que atiende esta app) filtra además por USUARIO (cada quien ve solo
    // lo suyo) y no trae la columna "Gestion" (queda vacía) — ver BASE_DE_DATOS.md.
    Task<DataTable> ConsultarAsync(string cia, string tipo, string numeroDocumento, string prestador,
        string radicado, string fecha, string usuario);

    // sp_MIP_ActualizaFormatoCnt (@CIA, @mipres, @obj, @estado, @usuario, @radicado) — el
    // UPDATE real solo usa CIA y RADICADO en el WHERE, @usuario no se usa dentro del SP (se
    // envía igual, por fidelidad con JuntasFormatoCntCO2.aspx.vb). El control de qué roles
    // pueden llamar esto vive en FormatosService, no en el SP (que no lo valida).
    Task ActualizarGestionAsync(string cia, string mipres, string obj, string estado, string usuario, string radicado);

    // El SP de consulta no expone EMAIL como columna propia para la rama de gestión (solo
    // lo usa internamente para armar el HTML del enlace "Gestion") — se consulta aparte,
    // parametrizado, mismo criterio que BuscarPrestadorAsync.
    Task<string?> ConsultarEmailAsync(string cia, string radicado);
}
