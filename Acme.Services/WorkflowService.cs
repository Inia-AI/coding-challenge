using Acme.Entities.Documents;
using Acme.Entities.Workflows;
using Acme.Entities.Workflows.Enums;
using Microsoft.Extensions.Logging;

namespace Acme.Services;

public class WorkflowService
{
    private readonly ILogger _logger;

    internal WorkflowService(ILogger<WorkflowService> logger)
    {
        _logger = logger;
    }

    public async Task LoadContextToWorkflowAsync(
        ICollection<DocumentInfo> documentInfos,
        Workflow workflow,
        int? blockIndex = null,
        Func<string?, Task>? onDocumentProcessed = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading context to workflow handler with {CountDocuments} documents.", documentInfos.Count);

        await DocumentAnalysisService.SetupPromptsAsync();
        await ExcelSheetAnalysisService.SetupPromptsAsync();

        List<Block> allBlocks = workflow.GetAllBlocks();
        foreach (Block block in allBlocks)
        {
            block.Documents.Clear();
        }

        foreach (DocumentInfo documentInfo in documentInfos)
        {
            if (documentInfo.Document is null)
            {
                _logger.LogError("Document {DocumentId} is null.", documentInfo.DocumentId);
                continue;
            }

            _logger.LogInformation("Loading document {DocumentName} to workflow.", documentInfo.Document.Name);

            AppendPagesToWorkflow(workflow, documentInfo.Document);
            AttachDocumentToBlocks(workflow, blockIndex, documentInfo);

            _logger.LogInformation("Document {DocumentName} processed.", documentInfo.Document.Name);
        }

        if (workflow.WorkflowUsesTopics())
        {
            _logger.LogInformation("Workflow uses topics, generating topics.");
            workflow.AllPagesTopics ??= await DocumentAnalysisService.GenerateTopicsFromOverviewsAsync(workflow.AllPages);
        }

        if (workflow.WorkflowUsesToc())
        {
            _logger.LogInformation("Workflow uses auto-detected TOC, generating TOC.");
            await GenerateTocsAsync(documentInfos, workflow, onDocumentProcessed);
        }
    }

    private async Task GenerateTocsAsync(ICollection<DocumentInfo> documentInfos, Workflow workflow, Func<string?, Task>? onDocumentProcessed)
    {
        var autoDetectedTocRags = workflow.Children.OrderBy(c => c.Order)
            .SelectMany(c => c.Blocks.OrderBy(b => b.Order)).SelectMany(b => b.RagSettings)
            .Where(r => r.Type is RagTypes.UseAutoDetectedTableOfContents).ToList();

        List<Document> documents = SelectDocumentsForToc(documentInfos, autoDetectedTocRags);
        if (documents.Count == 0)
        {
            _logger.LogInformation("No documents to generate TOC for.");
            return;
        }

        Dictionary<string, string?> tocModelsMap = GetTocModelsMap(documentInfos, autoDetectedTocRags);
        foreach (Document document in documents)
        {
            if (tocModelsMap.TryGetValue(document.Id, out string? tocModel) && tocModel is not null)
            {
                _logger.LogInformation("Generating TOC for document {DocumentName} with model {TocModel}.",
                    document.Name, tocModel);
            }
            else
            {
                _logger.LogWarning("No TOC model found for document {DocumentName}.", document.Name);
                return;
            }

            DocumentAnalysisService.GenerateTocUsingModelToc(document, tocModel);
            if (onDocumentProcessed is not null)
            {
                await onDocumentProcessed(document.Name);
            }
        }
    }

    private Dictionary<string, string?> GetTocModelsMap(ICollection<DocumentInfo> documentInfos, List<RagSettings> autoDetectedTocRags)
    {
        _logger.LogInformation("Getting TOC models map for {CountDocuments} documents.", documentInfos.Count);

        Dictionary<string, string?> tocModelsMap = [];
        foreach (DocumentInfo documentInfo in documentInfos)
        {
            _logger.LogTrace("Searching for TOC models for document {DocumentName}.", documentInfo.Document?.Name);

            foreach (RagSettings rag in autoDetectedTocRags)
            {
                if (rag.Block is null)
                {
                    throw new InvalidOperationException("Block is null.");
                }

                if (!documentInfo.CanBeProcessedForBlock(rag.Block))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(rag.TableOfContentsInput))
                {
                    _logger.LogWarning("Auto TOC RAG settings for block {BlockName} do not have a TOC model.", rag.Block?.Name);
                    continue;
                }

                tocModelsMap[documentInfo.DocumentId] = rag.TableOfContentsInput;
                break;
            }

            if (tocModelsMap.TryGetValue(documentInfo.DocumentId, out string? tocModel))
            {
                _logger.LogInformation("TOC model for document {DocumentName} found: {TocModel}.", documentInfo.Document?.Name, tocModel);
            }
            else
            {
                _logger.LogInformation("TOC model for document {DocumentName} not found.", documentInfo.Document?.Name);
            }
        }

        _logger.LogInformation("TOC models map for {CountDocuments} documents obtained.", documentInfos.Count);
        return tocModelsMap;
    }

    private List<Document> SelectDocumentsForToc(ICollection<DocumentInfo> documentInfos, List<RagSettings> autoDetectedTocRags)
    {
        List<Document> documents = [];
        foreach (DocumentInfo documentInfo in documentInfos)
        {
            if (documentInfo.Document is null)
            {
                _logger.LogError("Document {DocumentId} is null.", documentInfo.DocumentId);
                continue;
            }

            foreach (RagSettings rag in autoDetectedTocRags)
            {
                if (rag.Block is null)
                {
                    throw new InvalidOperationException("Block is null.");
                }

                if (documentInfo.CanBeProcessedForBlock(rag.Block))
                {
                    documents.Add(documentInfo.Document);
                    break;
                }
            }
        }

        return documents;
    }

    // The functionality here could be moved inside AttachDocumentToBlocks to take place right after
    // a given document is attached to a block, but accessing block.Workflow.AllPages would be awkward
    // since block.Workflow is nullable. Decided to keep it as it is for this sole reason.
    private void AppendPagesToWorkflow(Workflow workflow, Document document)
    {
        if (workflow.AllPages.Any(p => p.DocumentId == document.Id))
        {
            _logger.LogInformation("Document {DocumentName}'s pages are already in the workflow.", document.Name);
            return;
        }

        workflow.AllPages.AddRange(document.Pages);
        _logger.LogInformation("Document {DocumentName}'s pages added to the workflow.", document.Name);
    }

    private void AttachDocumentToBlocks(Workflow workflow, int? blockIndex, DocumentInfo documentInfo)
    {
        // Assuming the top level workflow can only have one level of children, as implied by GetAllBlocks in Workflow,
        // this could potentially be replaced with a Select that selects children workflows to avoid recursion
        foreach (Workflow child in workflow.Children)
        {
            AttachDocumentToBlocks(child, blockIndex, documentInfo);
        }

        // Instead of checking for a non-negative value, check for non-null which I think is easier to follow
        if (blockIndex.HasValue)
        {
            Block block = workflow[blockIndex.Value];
            LoadDocumentToBlock(block, documentInfo);
            return;
        }

        // Load document to all blocks if blockIndex is null
        foreach (Block block in workflow.Blocks)
        {
            LoadDocumentToBlock(block, documentInfo);
        }
    }

    private void LoadDocumentToBlock(Block block, DocumentInfo documentInfo)
    {
        if (documentInfo.Document is null)
        {
            _logger.LogError("Document {documentId} is null.", documentInfo.DocumentId);
            return;
        }

        if (!documentInfo.CanBeProcessedForBlock(block) || !block.ShouldUseDocuments())
        {
            _logger.LogDebug("Document {DocumentName} cannot be processed for block {BlockName}.", documentInfo.Document.Name, block.Name);
            return;
        }

        if (block.RagSettings.Count == 0)
        {
            _logger.LogWarning("Block {BlockName} does not have RAG settings.", block.Name);
            return;
        }

        if (block.Documents.Any(d => d.Id == documentInfo.DocumentId))
        {
            _logger.LogDebug("Document {DocumentName} is already attached to block {BlockName}.", documentInfo.Document.Name, block.Name);
            return;
        }

        _logger.LogInformation("Attaching document {DocumentName} to block {BlockName}.", documentInfo.Document.Name, block.Name);
        block.Documents.Add(documentInfo.Document);
    }
}
