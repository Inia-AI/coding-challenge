using Aspose.Cells;
using CsvHelper;
using Acme.Common.Enums;
using Acme.Core;
using Acme.Entities.Documents;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using PdfDocument = Aspose.Pdf.Document;

namespace Acme.Services;

public class DocumentProcessingService
{
    private readonly ILogger _logger;

    internal DocumentProcessingService(ILogger<DocumentProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessDocumentsAsync(
        List<Document?> documents,
        bool shouldGenerateOverviews = true,
        bool shouldDetectSectionTitles = true,
        Func<string?, Task>? onDocumentProcessed = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing {CountDocuments} documents.", documents.Count);

        await DocumentAnalysisService.SetupPromptsAsync();
        await ExcelSheetAnalysisService.SetupPromptsAsync();

        foreach (Document? document in documents)
        {
            if (document is null)
            {
                _logger.LogError("Document is null.");
                continue;
            }

            _logger.LogInformation("Processing document {DocumentName}.", document.Name);

            await ProcessPagesAsync(document, shouldGenerateOverviews, shouldDetectSectionTitles);
            if (onDocumentProcessed is not null)
            {
                await onDocumentProcessed(document.Name);
            }

            _logger.LogInformation("Document {DocumentName} processed.", document.Name);
        }

        _logger.LogInformation("Documents processed.");
    }

    private async Task ProcessPagesAsync(Document document, bool shouldGenerateOverviews, bool shouldDetectSectionTitles)
    {
        await (document.MediaType switch
        {
            MediaType.ApplicationPdf => ProcessPdfPagesAsync(document, shouldGenerateOverviews, shouldDetectSectionTitles),
            MediaType.ImageJpeg => ProcessMediaPagesAsync(document, shouldUseOcr: false, shouldGenerateOverviews, shouldDetectSectionTitles),
            MediaType.ImagePng => ProcessMediaPagesAsync(document, shouldUseOcr: false, shouldGenerateOverviews, shouldDetectSectionTitles),
            MediaType.TextCsv => ProcessCsvPagesAsync(document),
            MediaType.ApplicationVndMsExcel => ProcessExcelPagesAsync(document, shouldGenerateOverviews),
            MediaType.ApplicationVndOpenXmlFormatsOfficeDocumentSpreadsheetMlSheet => ProcessExcelPagesAsync(document, shouldGenerateOverviews),
            _ => throw new InvalidOperationException("Document media type is unknown.")
        });
    }

    private async Task ProcessCsvPagesAsync(Document document)
    {
        _logger.LogInformation("Processing CSV document {DocumentName}.", document.Name);
        if (document.File is null)
        {
            _logger.LogError("Document {DocumentName} does not have a File.", document.Name);
            return;
        }

        using var memoryStream = new MemoryStream(document.File.Bytes);
        Page page = document[1];
        page.RawText = Encoding.UTF8.GetString(memoryStream.ToArray());
        var stringBuilder = new StringBuilder(page.RawText.Length * 2);
        using var reader = new StreamReader(memoryStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        string[] headerRecord = [];

        if (await csv.ReadAsync())
        {
            if (csv.HeaderRecord != null)
            {
                headerRecord = csv.HeaderRecord;
            }
            else
            {
                headerRecord = new string[csv.ColumnCount];
                for (int i = 0; i < csv.ColumnCount; i++)
                {
                    headerRecord[i] = csv.GetField(i) ?? string.Empty;
                }
            }
        }

        while (await csv.ReadAsync())
        {
            for (int i = 0; i < csv.ColumnCount; i++)
            {
                string? value = csv.GetField(i)?.Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    stringBuilder.Append(headerRecord[i]);
                    stringBuilder.Append(": ");
                    stringBuilder.Append(csv.GetField(i));
                    stringBuilder.AppendLine();
                }
            }

            stringBuilder.AppendLine();
        }

        page.RawText = stringBuilder.ToString();
        await ProcessEmbeddingsForPageAsync(page);
    }

    private async Task ProcessExcelPagesAsync(Document document, bool shouldGenerateOverviews)
    {
        _logger.LogInformation("Processing Excel document {DocumentName}.", document.Name);
        if (document.File is null)
        {
            _logger.LogError("Document {DocumentName} does not have a File.", document.Name);
            return;
        }

        if (document.Pages.Count == 0)
        {
            using var memoryStream = new MemoryStream(document.File.Bytes);
            var workbook = new Workbook(memoryStream);
            await ExcelSheetAnalysisService.ChunkSheetsAsync(document, workbook);
        }
        else
        {
            _logger.LogInformation("Document {DocumentName} already has pages.", document.Name);
        }

        if (shouldGenerateOverviews)
        {
            await DocumentAnalysisService.GenerateOverviewsAsync(document);
        }

        await ProcessEmbeddingsAsync(document);
    }

    private async Task ProcessMediaPagesAsync(Document document, bool shouldUseOcr, bool shouldGenerateOverviews, bool shouldDetectSectionTitles)
    {
        _logger.LogInformation("Processing media document {DocumentName}.", document.Name);
        if (document.File is null)
        {
            _logger.LogError("Document {DocumentName} does not have a File.", document.Name);
            return;
        }

        Page page = document[1];
        page.Image ??= new(new BinaryData(document.File.Bytes), document.MediaType);
        if (!shouldUseOcr)
        {
            return;
        }

        _logger.LogInformation("Processing media document {DocumentName} with OCR.", document.Name);
        string? rawText = shouldUseOcr ? throw new NotImplementedException() : null;
        page.RawText ??= rawText?.Replace("\0", "");
        page.Text ??= rawText?.Replace("\0", "");

        if (shouldGenerateOverviews)
        {
            await DocumentAnalysisService.GenerateOverviewsAsync(document);
        }

        if (shouldDetectSectionTitles)
        {
            await DocumentAnalysisService.DetectSectionTitlesAsync(document);
        }

        if (IsPageEmbeddingValid(page))
        {
            _logger.LogDebug("Document {DocumentName} already has an embedding for page {PageNumber}.", document.Name, page.PageNumber);
            return;
        }

        if (string.IsNullOrWhiteSpace(page.Overview))
        {
            _logger.LogError("Failed to get overview for page {PageNumber}.", page.PageNumber);
        }

        await ProcessEmbeddingsForPageAsync(page);
    }

    private async Task ProcessPdfPagesAsync(Document document, bool shouldGenerateOverviews, bool shouldDetectSectionTitles)
    {
        _logger.LogInformation("Processing PDF document {DocumentName}.", document.Name);
        if (document.File is null)
        {
            _logger.LogError("Document {DocumentName} does not have a File.", document.Name);
            return;
        }

        await GetPdfPagesRawTextsAsync(document);

        if (shouldGenerateOverviews)
        {
            await DocumentAnalysisService.GenerateOverviewsAsync(document);
        }

        if (shouldDetectSectionTitles)
        {
            await DocumentAnalysisService.DetectSectionTitlesAsync(document);
        }

        await ProcessEmbeddingsAsync(document);
    }

    private async Task ProcessEmbeddingsAsync(Document document)
    {
        var pagesToEmbed = document.Pages.Where(p => !IsPageEmbeddingValid(p) && !string.IsNullOrWhiteSpace(p.Overview)).ToList();
        int countAlreadyEmbedded = document.Pages.Where(p => p.EmbeddingVector is not null).Count();
        int countErroneous = document.Pages.Where(p => p.EmbeddingVector?.Model == EmbeddingsConnector.ERROR_VECTOR_MODEL).Count();

        _logger.LogInformation(
            "Document {DocumentName} has {CountPages} pages, {CountAlreadyEmbedded} pages already embedded, {CountErroneous} pages with erroneous embeddings, {CountToEmbed} pages to embed.",
            document.Name, document.Pages.Count, countAlreadyEmbedded, countErroneous, pagesToEmbed.Count);

        if (pagesToEmbed.Count == 0)
        {
            return;
        }

        foreach (Page page in pagesToEmbed)
        {
            await ProcessEmbeddingsForPageAsync(page);
        }
    }

    private async Task ProcessEmbeddingsForPageAsync(Page page)
    {
        var pageText = page.Overview ?? "An overview for this content is currently unavailable";
        // In this exercise version of the code, embedding vector is never null due to pageText containing a fallback message in the case of page.Overview
        // being null, but the original code probably can return null embeddings, so I am keeping it as nullable here as well
        double[]? vector = (await EmbeddingsConnector.GetResponseAsync([pageText])).FirstOrDefault();
        string modelName = page.Overview is null || vector is null ? EmbeddingsConnector.ERROR_VECTOR_MODEL : EmbeddingsConnector.MODEL_NAME;

        if (modelName == EmbeddingsConnector.ERROR_VECTOR_MODEL)
        {
            _logger.LogWarning("Failed to get embedding for page {PageNumber}.", page.PageNumber);
        }

        EmbeddingVector embeddingVector = page.EmbeddingVector ?? new EmbeddingVector(modelName, []);
        embeddingVector.Model = modelName;
        embeddingVector.Vector = vector ?? [];
        page.EmbeddingVector = embeddingVector;
    }

    private static async Task GetPdfPagesRawTextsAsync(Document document)
    {
        if (document.File is null)
        {
            throw new InvalidOperationException("Document file is null.");
        }

        using var memoryStream = new MemoryStream(document.File.Bytes);
        using PdfDocument pdf = new(memoryStream);
        Dictionary<int, string> pagesTexts = await PdfExtractor.ExtractPaginatedTextAsync(pdf, Enumerable.Range(1, pdf.Pages.Count));
        foreach ((int pageNumber, string pageText) in pagesTexts)
        {
            Page page = document[pageNumber];
            page.RawText ??= pageText.Replace("\0", "");
        }
    }

    private static bool IsPageEmbeddingValid(Page page)
    {
        return page.EmbeddingVector is not null && page.EmbeddingVector.Model != EmbeddingsConnector.ERROR_VECTOR_MODEL;
    }
}
