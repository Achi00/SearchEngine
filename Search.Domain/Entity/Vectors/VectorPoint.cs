namespace Search.Domain.Entity.Vectors
{
    public class VectorPoint
    {
        public ulong Id { get; set; }
        public float[] Vector { get; set; } = default!;
        public Dictionary<string, object>? Payload { get; set; }
    }
}
