using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace Acme.Common;

public abstract class JsonSerializable<T> where T : class
{
    public static T? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            json = SanitizeJson(json);

            return JsonConvert.DeserializeObject<T>(json);
        }
        catch { }

        return null;
    }

    public virtual string ToJson(bool pretty)
    {
        return JsonConvert.SerializeObject(this, pretty ? Formatting.Indented : Formatting.None)
        .Replace("\r\n", "\n")
        .Replace("\r", "\n");
    }

    public override string ToString()
    {
        return ToJson(false);
    }

    public static string GetJsonSchema()
    {
        var generator = new JSchemaGenerator();
        JSchema schema = generator.Generate(typeof(T));

        return schema.ToString();
    }

    private static string SanitizeJson(string text)
    {
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');

        return start < 0 || end < 0
            ? text
            : text[start..(end + 1)];
    }
}
