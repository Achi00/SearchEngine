namespace Search.Domain.Entity.Vectors
{
    public class SearchResult
    {
        public ulong Id { get; set; }
        public float Score { get; set; }
        public Dictionary<string, object>? Payload { get; set; }
    }
}
