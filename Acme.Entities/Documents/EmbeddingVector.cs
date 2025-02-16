namespace Acme.Entities.Documents;

public class EmbeddingVector
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Model { get; set; } = string.Empty;
    public double[] Vector { get; set; } = [];

    public EmbeddingVector()
    {
    }

    public EmbeddingVector(string model, double[] vector)
    {
        Model = model;
        Vector = vector;
    }
}
