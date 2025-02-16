namespace Acme.Entities.Documents;

public class File
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public byte[] Bytes { get; set; } = [];

    internal File()
    {
    }

    public File(string name, byte[] bytes)
    {
        Name = name;
        Bytes = bytes;
    }
}
