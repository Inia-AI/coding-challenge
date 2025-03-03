using Acme.Common;
using Acme.Entities.Documents;

namespace Acme.Entities.Workflows;

public class Workflow : JsonSerializable<Workflow>
{
    public string Id { get; internal set; } = Guid.NewGuid().ToString();
    public ICollection<Workflow> Children { get; private set; } = [];
    public ICollection<Block> Blocks { get; private set; } = [];
    public List<Page> AllPages { get; } = [];
    public int Order { get; set; }
    public Workflow? Parent { get; private set; }
    public string? AllPagesTopics { get; set; }

    public Workflow()
    {
    }

    public Block this[int index] => Blocks.Where(q => q.Order == index).FirstOrDefault()
            ?? throw new IndexOutOfRangeException($"Block at index {index} not found.");

    public List<Block> GetAllBlocks()
    {
        Workflow topLevelWorkflow = GetTopLevelWorkflow();

        return [.. topLevelWorkflow.Blocks, .. topLevelWorkflow.Children.SelectMany(q => q.Blocks)];
    }

    public Workflow GetTopLevelWorkflow()
    {
        Workflow topLevelWorkflow = this;

        while (topLevelWorkflow.Parent is not null)
        {
            topLevelWorkflow = topLevelWorkflow.Parent;
        }

        return topLevelWorkflow;
    }

}
