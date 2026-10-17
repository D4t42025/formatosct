using System.Data;
using ClosedXML.Excel;

namespace Contingencia.Web.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] AXlsx(DataTable tabla, string nombreHoja)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(string.IsNullOrWhiteSpace(nombreHoja) ? "Datos" : nombreHoja);
        hoja.Cell(1, 1).InsertTable(tabla);
        hoja.Columns().AdjustToContents();
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
