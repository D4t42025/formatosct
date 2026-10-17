using System.Data;

namespace Contingencia.Infrastructure.Filtros;

// sp_DQS_ConsultaFiltros: procedimiento "multiplexor" usado por decenas de páginas legadas
// para llenar combos/listas/informes (mismo objeto de base de datos que ya usa Recobros vía
// Imperium.Infrastructure.Filtros.FiltrosRepository — port exacto). El significado de cada
// columna del resultado y de varios parámetros cambia según "valida" (equivalente al
// "INTVALIDA" que usa cada página). Contingencia lo usa para @INTVALIDA=136 (tokens del WS
// de prescripciones MIPRES, ver Formatos/MipresPrescripcionValidator) y para @INTVALIDA=138
// (catálogo CAMPO/CODIGO/DESCRIPCION de DQS_MIP_CRUZAR — municipios, vías de administración,
// formas farmacéuticas, etc.; ver Juntas/JuntasService.CargarCruzarAsync y
// NuevaContingencia.razor.OnInitializedAsync).
public interface IFiltrosRepository
{
    Task<DataTable> ConsultarAsync(
        string cia,
        int valida,
        string usuario = "",
        string tipo = "",
        string paquete = "",
        string factura = "",
        string servicio = "",
        string opcion = "",
        string estado = "",
        string regimen = "",
        string sucursal = "",
        string tiposervicio = "",
        string ente = "");
}
