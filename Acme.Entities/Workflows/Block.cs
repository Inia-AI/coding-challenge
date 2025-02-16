using Acme.Common;
using Acme.Entities.Documents;
using Acme.Entities.Workflows.Enums;

namespace Acme.Entities.Workflows;

public class Block : JsonSerializable<Block>
{
    public string Id { get; internal set; } = Guid.NewGuid().ToString();
    public ICollection<RagSettings> RagSettings { get; private set; } = [];
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public BlockTypes Type { get; set; } = BlockTypes.AiQuery;
    public List<string> SupportedDocumentClasses { get; set; } = [];
    public List<Document> Documents { get; } = [];
    public Workflow? Workflow { get; set; }
    public string? ReplacementTag { get; set; }
    public bool ShouldUsePageImages { get; set; }

    public Block()
    {
    }

    public Block(Workflow workflow)
    {
        Workflow = workflow;
        Workflow.Blocks.Add(this);
    }

    public bool ShouldUseDocuments()
    {
        return Type is BlockTypes.AiQuery
            or BlockTypes.SimpleRag;
    }
}
