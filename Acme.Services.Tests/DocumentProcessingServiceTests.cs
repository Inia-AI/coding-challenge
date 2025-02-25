using Acme.Common;
using Acme.Common.Enums;
using Acme.Entities.Documents;
using Acme.Entities.Workflows;
using Acme.Entities.Workflows.Enums;
using Acme.Testing;
using Xunit.Abstractions;
using File = Acme.Entities.Documents.File;

namespace Acme.Services.Tests;

public class DocumentProcessingServiceTests
{
    private readonly DocumentProcessingService _documentProcessing;
    private readonly WorkflowService _workflowService;
    private readonly BinaryContent _imageSample;
    private readonly BinaryContent _pdfSample;
    private readonly BinaryContent _csvSample;
    private readonly BinaryContent _excelSample;

    // WorkflowService tests should be separate from DocumentProcessingService tests, however for the sake of
    // keeping the refactor scope limited to the service layer, workflow tests will be done here
    public DocumentProcessingServiceTests(
        ITestOutputHelper testOutputHelper)
    {
        _documentProcessing = new DocumentProcessingService(TestsLogger.CreateLogger<DocumentProcessingService>(testOutputHelper));
        _workflowService = new WorkflowService(TestsLogger.CreateLogger<WorkflowService>(testOutputHelper));

        _imageSample = new(
            BinaryData.FromBytes(System.IO.File.ReadAllBytes("TestResources/ManagerQs1.png")),
            MediaType.ImagePng);
        _pdfSample = new(
            BinaryData.FromBytes(System.IO.File.ReadAllBytes("TestResources/MultiPageSearchable.pdf")),
            MediaType.ApplicationPdf);
        _csvSample = new(
            BinaryData.FromBytes(System.IO.File.ReadAllBytes("TestResources/csv.csv")),
            MediaType.TextCsv);
        _excelSample = new(
            BinaryData.FromBytes(System.IO.File.ReadAllBytes("TestResources/excel.xlsx")),
            MediaType.ApplicationVndMsExcel);
    }

    [Fact]
    public async Task ProcessDocumentsAsync_MultipleFiles_ProcessesCorrectlyAsync()
    {
        // Arrange
        string? onDocProcessedIndicator = null;
        File file1 = new("MultiPageSearchable.pdf", _pdfSample.BinaryData.ToArray());
        File file2 = new("excel.xlsx", _excelSample.BinaryData.ToArray());
        File file3 = new("ManagerQs1.png", _imageSample.BinaryData.ToArray());
        File file4 = new("csv.csv", _csvSample.BinaryData.ToArray());
        List<Document?> documents =
        [
            new() { MediaType = MediaType.ApplicationPdf, File = file1 },
            new() { MediaType = MediaType.ApplicationVndMsExcel, File = file2 },
            new() { MediaType = MediaType.ImagePng, File = file3 },
            new() { MediaType = MediaType.TextCsv, File = file4 },
        ];

        // Act
        await _documentProcessing.ProcessDocumentsAsync(
            documents,
            shouldGenerateOverviews: true,
            onDocumentProcessed: async document => await Task.Run(() => onDocProcessedIndicator += "success|"));

        // Assert
        Assert.Equal("success|success|success|success|", onDocProcessedIndicator);
        Assert.Equal(10, documents[0]?.Pages.Count);
        Assert.Contains("Page 1", documents[0]?.Pages.FirstOrDefault()?.RawText);
        Assert.False(string.IsNullOrWhiteSpace(documents[0]?.Pages.FirstOrDefault()?.Overview));
        Assert.NotNull(documents[0]?.Pages.FirstOrDefault()?.EmbeddingVector);
        Assert.DoesNotContain("ERROR", documents[0]?.Pages.FirstOrDefault()?.EmbeddingVector?.Model);

        Assert.Equal(1, documents[1]?.Pages.Count);
        Assert.Contains("Sheet1", documents[1]?.Pages.FirstOrDefault()?.RawText);
        Assert.False(string.IsNullOrWhiteSpace(documents[1]?.Pages.FirstOrDefault()?.Overview));
        Assert.NotNull(documents[1]?.Pages.FirstOrDefault()?.EmbeddingVector);
        Assert.DoesNotContain("ERROR", documents[1]?.Pages.FirstOrDefault()?.EmbeddingVector?.Model);

        Assert.Equal(1, documents[2]?.Pages.Count);
        Assert.Null(documents[2]?.Pages.FirstOrDefault()?.EmbeddingVector);
        Assert.Null(documents[2]?.Pages.FirstOrDefault()?.Overview);

        Assert.Equal(1, documents[3]?.Pages.Count);
        Assert.Contains("Header1: Value1", documents[3]?.Pages.FirstOrDefault()?.RawText);
        Assert.False(string.IsNullOrWhiteSpace(documents[3]?.Pages.FirstOrDefault()?.Overview));
        Assert.NotNull(documents[3]?.Pages.FirstOrDefault()?.EmbeddingVector);
        Assert.DoesNotContain("ERROR", documents[0]?.Pages.FirstOrDefault()?.EmbeddingVector?.Model);
    }

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
        File file1 = new("MultiPageSearchable.pdf", _pdfSample.BinaryData.ToArray());
        File file2 = new("excel.xlsx", _excelSample.BinaryData.ToArray());
        File file3 = new("ManagerQs1.png", _imageSample.BinaryData.ToArray());
        File file4 = new("csv.csv", _csvSample.BinaryData.ToArray());
        Document document1 = new() { MediaType = MediaType.ApplicationPdf, File = file1, Name = "MultiPageSearchable.pdf" };
        Document document2 = new() { MediaType = MediaType.ApplicationVndMsExcel, File = file2, Name = "excel.xlsx" };
        Document document3 = new() { MediaType = MediaType.ImagePng, File = file3, Name = "ManagerQs1.png" };
        Document document4 = new() { MediaType = MediaType.TextCsv, File = file4, Name = "csv.csv" };
        List<DocumentInfo> documents =
        [
            new DocumentInfo() { Document = document1, DocumentClass = "MultiPageSearchablePdf" },
            new DocumentInfo() { Document = document2, DocumentClass = "xlsx" },
            new DocumentInfo() { Document = document3 },
            new DocumentInfo() { Document = document4 },
        ];

        // In order to properly test LoadContextToWorkflowAsync this shouldn't be here
        // Resulting documents after processing should be mocked here instead
        await _documentProcessing.ProcessDocumentsAsync(
            [.. documents.Select(d => d.Document)],
            shouldGenerateOverviews: false,
            shouldDetectSectionTitles: false);

        // Act
        await _workflowService.LoadContextToWorkflowAsync(
            documents,
            workflow,
            onDocumentProcessed: async document => await Task.Run(() => onDocProcessedIndicator += "success|"));

        // Assert
        Assert.Null(onDocProcessedIndicator);

        _ = Assert.Single(block1.Documents);
        Assert.Equal("MultiPageSearchable.pdf", block1.Documents[0].Name);
        Assert.Null(block1.Documents.Where(d => d.Name.Contains("WithGraphicsSearchable")).FirstOrDefault());
        Assert.Null(block1.Documents.Where(d => d.Name.Contains("ManagerQs1")).FirstOrDefault());
        Assert.Equal(10, block1.Documents[0].Pages.Count);
        Assert.Contains("Page 1", block1.Documents[0].Pages.FirstOrDefault()?.RawText);

        Assert.Equal(4, block2.Documents.Count);
        Assert.Contains(block2.Documents, d => d.Name.Contains("excel"));
        Assert.Contains(block2.Documents, d => d.Name.Contains("ManagerQs1"));
        Assert.Contains(block2.Documents, d => d.Name.Contains("MultiPageSearchable"));
        Assert.Contains(block2.Documents, d => d.Name.Contains("csv"));

        Assert.Equal(2, block3.Documents.Count);
        Assert.Contains("Page 1", block3.Documents[0].Pages.FirstOrDefault()?.RawText);
        Assert.Null(block3.Documents.Where(d => d.Name.Contains("ManagerQs1")).FirstOrDefault());

        Assert.Empty(block4.Documents);
        Assert.Null(workflow.AllPagesTopics);
    }
}
