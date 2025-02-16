using Acme.Entities.Documents;

namespace Acme.Services;

public class DocumentAnalysisService
{
    internal DocumentAnalysisService()
    {
    }

    public static async Task SetupPromptsAsync()
    {
        await Task.CompletedTask;
    }

    public static async Task<string?> GenerateTopicsFromOverviewsAsync(List<Page> pages)
    {
        await Task.CompletedTask;

        return "Topics";
    }

    public static async Task GenerateOverviewsAsync(Document document, bool forHfMonitoring = false)
    {
        foreach (Page page in document.Pages)
        {
            page.Overview = "Overview";
        }

        await Task.CompletedTask;
    }

    public static async Task DetectSectionTitlesAsync(Document document)
    {
        await Task.CompletedTask;
    }

    public static void GenerateTocUsingModelToc(Document document, string modelToc)
    {
    }
}
