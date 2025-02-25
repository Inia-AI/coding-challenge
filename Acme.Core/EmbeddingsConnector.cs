namespace Acme.Core;

public class EmbeddingsConnector
{
    public const string MODEL_NAME = "Cohere";
    public const string ERROR_VECTOR_MODEL = "ERROR";

    public static async Task<List<double[]>> GetResponseAsync(List<string> documents)
    {
        List<double[]> embeddings = [];

        foreach (string document in documents)
        {
            embeddings.Add([0, MODEL_NAME.Length, document.Length]);
        }

        await Task.CompletedTask;

        return embeddings;
    }
}
