using System.Runtime.Serialization;

namespace Acme.Entities.Workflows.Enums;

public enum BlockTypes
{
    [EnumMember(Value = "AiQuery")]
    AiQuery = 0,

    [EnumMember(Value = "Merge")]
    Merge = 10,

    [EnumMember(Value = "SimpleRag")]
    SimpleRag = 9,
}
