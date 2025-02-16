using Acme.Entities.Workflows;

namespace Acme.Entities.Documents;

public partial class DocumentInfo
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = string.Empty;
    public Document? Document { get; set; }
    public string DocumentClass { get; set; } = string.Empty;

    public DocumentInfo()
    {
    }

    public bool CanBeProcessedForBlock(Block block)
    {
        return !block.SupportedDocumentClasses.Contains("None")
            && (block.SupportedDocumentClasses.Count == 0
            || block.SupportedDocumentClasses.Contains("Any")
            || block.SupportedDocumentClasses.Contains(DocumentClass));
    }
}
