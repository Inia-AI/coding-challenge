using Aspose.Cells;
using Acme.Entities.Documents;

namespace Acme.Services;

public class ExcelSheetAnalysisService
{
    public ExcelSheetAnalysisService()
    {
    }

    public static async Task SetupPromptsAsync()
    {
        await Task.CompletedTask;
    }

    public static async Task ChunkSheetsAsync(
        Document document,
        Workbook workbook,
        int chunkSize = -1)
    {
        foreach (Worksheet sheet in workbook.Worksheets)
        {
            Page page = document[document.Pages.Count + 1];
            page.RawText = sheet.Name;
        }

        await Task.CompletedTask;
    }
}
