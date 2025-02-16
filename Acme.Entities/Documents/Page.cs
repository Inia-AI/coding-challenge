using Acme.Common;

namespace Acme.Entities.Documents;

public class Page
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public Document? Document { get; set; }
    public EmbeddingVector? EmbeddingVector { get; set; }
    public int PageNumber { get; init; }
    public string? RawText { get; set; }
    public string? Text { get; set; }
    public string? Overview { get; set; }
    public BinaryContent? Image { get; set; }
    public string DocumentId { get; set; } = string.Empty;

    internal Page()
    {
    }
}
