using Acme.Common;
using Acme.Common.Enums;
using Acme.Entities.Documents;
using Acme.Entities.Workflows;
using Acme.Entities.Workflows.Enums;
using Acme.Testing;
using Xunit.Abstractions;
using File = Acme.Entities.Documents.File;

namespace Acme.Services.Tests;

public class WorkflowServiceTests
{
    private readonly WorkflowService _workflowService;

    public WorkflowServiceTests(ITestOutputHelper testOutputHelper)
    {
        _workflowService = new WorkflowService(TestsLogger.CreateLogger<WorkflowService>(testOutputHelper));
    }

    /*
     * I changed this test around a bit so that it does not depend on DocumentProcessingService to process documents and pages.
     * Instead, workflow is loaded with documents whose pages are initialized by hand to isolate workflow context loading operation
     * from document processing. Also added some more assertions to cover more cases.
     */
    [Fact]
    public async Task LoadContextToWorkflowAsync_MultipleFilesAndFileTypesMultipleQueries_FillsWorkflowAsync()
    {
        // Arrange
        string? onDocProcessedIndicator = null;
        Workflow workflow = new();

        Block block1 = new(workflow) { Name = "Block 1", ReplacementTag = "block1", ShouldUsePageImages = true };
        block1.SupportedDocumentClasses.Add("MultiPageSearchablePdf");
        _ = new RagSettings(block1) { Type = RagTypes.WholeDocument };

        Block block2 = new(workflow) { Name = "Block 2", ReplacementTag = "block2", ShouldUsePageImages = true };
        _ = new RagSettings(block2) { Type = RagTypes.WholeDocument };

        Block block3 = new(workflow) { Name = "Block 3", ReplacementTag = "block3", ShouldUsePageImages = true };
        block3.SupportedDocumentClasses.Add("MultiPageSearchablePdf");
        block3.SupportedDocumentClasses.Add("xlsx");
        _ = new RagSettings(block3) { Type = RagTypes.WholeDocument };

        Block block4 = new(workflow) { Name = "Block 4", ReplacementTag = "block4", Type = BlockTypes.Merge };

        File file1 = new("pdf.pdf", []);
        Document document1 = new() { MediaType = MediaType.ApplicationPdf, File = file1, Name = "pdf.pdf" };
        document1[1].RawText = "pdf page1";
        document1[2].RawText = "pdf page2";

        File file2 = new("excel.xlsx", []);
        Document document2 = new() { MediaType = MediaType.ApplicationVndMsExcel, File = file2, Name = "excel.xlsx" };
        document2[1].RawText = "excel page1";

        File file3 = new("image.png", []);
        Document document3 = new() { MediaType = MediaType.ImagePng, File = file3, Name = "image.png" };

        File file4 = new("csv.csv", []);
        Document document4 = new() { MediaType = MediaType.TextCsv, File = file4, Name = "csv.csv" };
        document4[1].RawText = "csv page1";

        List<DocumentInfo> documents =
        [
            new DocumentInfo() { Document = document1, DocumentClass = "MultiPageSearchablePdf" },
            new DocumentInfo() { Document = document2, DocumentClass = "xlsx" },
            new DocumentInfo() { Document = document3 },
            new DocumentInfo() { Document = document4 },
        ];

        // Act
        await _workflowService.LoadContextToWorkflowAsync(
            documents,
            workflow,
            onDocumentProcessed: async document => await Task.Run(() => onDocProcessedIndicator += "success|"));

        // Assert
        Assert.Null(onDocProcessedIndicator);

        _ = Assert.Single(block1.Documents);
        Assert.Equal("pdf.pdf", block1.Documents[0].Name);
        Assert.Null(block1.Documents.Where(d => d.Name.Equals("excel.xls")).FirstOrDefault());
        Assert.Null(block1.Documents.Where(d => d.Name.Equals("imgage.png")).FirstOrDefault());
        Assert.Null(block1.Documents.Where(d => d.Name.Equals("csv.csv")).FirstOrDefault());
        Assert.Equal(2, block1.Documents[0].Pages.Count);
        Assert.Contains("pdf page1", block1.Documents[0].Pages.FirstOrDefault()?.RawText);

        Assert.Equal(4, block2.Documents.Count);
        Assert.Contains(block2.Documents, d => d.Name.Equals("pdf.pdf"));
        Assert.Contains(block2.Documents, d => d.Name.Equals("excel.xlsx"));
        Assert.Contains(block2.Documents, d => d.Name.Equals("image.png"));
        Assert.Contains(block2.Documents, d => d.Name.Equals("csv.csv"));

        Assert.Equal(2, block3.Documents.Count);
        Assert.Contains("pdf page1", block3.Documents[0].Pages.FirstOrDefault()?.RawText);
        Assert.Contains("excel page1", block3.Documents[1].Pages.FirstOrDefault()?.RawText);
        Assert.Null(block3.Documents.Where(d => d.Name.Contains("image.png")).FirstOrDefault());
        Assert.Null(block3.Documents.Where(d => d.Name.Contains("csv.csv")).FirstOrDefault());

        Assert.Empty(block4.Documents);
        Assert.Null(workflow.AllPagesTopics);

        Assert.All(document1.Pages, page => Assert.Contains(page, workflow.AllPages));
        Assert.All(document2.Pages, page => Assert.Contains(page, workflow.AllPages));
        Assert.All(document3.Pages, page => Assert.Contains(page, workflow.AllPages));
        Assert.All(document4.Pages, page => Assert.Contains(page, workflow.AllPages));
    }
}
