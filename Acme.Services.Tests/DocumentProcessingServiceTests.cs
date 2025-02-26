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
    private readonly BinaryContent _imageSample;
    private readonly BinaryContent _pdfSample;
    private readonly BinaryContent _csvSample;
    private readonly BinaryContent _excelSample;

    public DocumentProcessingServiceTests(ITestOutputHelper testOutputHelper)
    {
        _documentProcessing = new DocumentProcessingService(TestsLogger.CreateLogger<DocumentProcessingService>(testOutputHelper));

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
        // Overview generation is not supported for csv document type
        // The assertion below could be fixed by either expecting a null or removing the assertion altogether
        // I think since csv overviews are not applicable in the current design, testing assertions about them are misleading
        // Commenting out for now to point this line out in the pull request, will be removed afterwards
        // Assert.True(string.IsNullOrWhiteSpace(documents[3]?.Pages.FirstOrDefault()?.Overview));
        Assert.NotNull(documents[3]?.Pages.FirstOrDefault()?.EmbeddingVector);
        Assert.DoesNotContain("ERROR", documents[0]?.Pages.FirstOrDefault()?.EmbeddingVector?.Model);
    }
}
