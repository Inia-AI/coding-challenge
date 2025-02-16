using Acme.Common;
using Acme.Entities.Workflows.Enums;

namespace Acme.Entities.Workflows;

public class RagSettings : JsonSerializable<RagSettings>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Block? Block { get; set; }
    public RagTypes Type { get; set; } = RagTypes.WholeDocument;
    public string? TableOfContentsInput { get; set; }

    public RagSettings()
    {
    }

    public RagSettings(Block block)
    {
        Block = block;
        Block.RagSettings.Add(this);
    }
}
