using Acme.Common.Enums;

namespace Acme.Common;

public class BinaryContent
{
    public BinaryData BinaryData { get; }
    public MediaType MediaType { get; }

    public BinaryContent(BinaryData binaryData, MediaType mediaType)
    {
        BinaryData = binaryData;
        MediaType = mediaType;
    }

    public BinaryContent(byte[] bytes, MediaType mediaType)
    {
        BinaryData = BinaryData.FromBytes(bytes);
        MediaType = mediaType;
    }
}
