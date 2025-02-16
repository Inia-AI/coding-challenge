using System.Runtime.Serialization;

namespace Acme.Entities.Workflows.Enums;

public enum RagTypes
{
    [EnumMember(Value = "WholeDocument")]
    WholeDocument = 0,

    [EnumMember(Value = "UseTopics")]
    UseTopics = 9,

    [EnumMember(Value = "UseAutoDetectedTableOfContents")]
    UseAutoDetectedTableOfContents = 10,
}
