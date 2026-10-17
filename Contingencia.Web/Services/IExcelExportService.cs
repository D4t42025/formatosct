using System.Data;

namespace Contingencia.Web.Services;

public interface IExcelExportService
{
    byte[] AXlsx(DataTable tabla, string nombreHoja);
}
