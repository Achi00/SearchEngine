namespace Search.Application.Options
{
    public class MeilisearchOptions
    {
        public const string SectionName = "Meilisearch";
        public string Url { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string IndexName { get; set; } = "products";
    }
}
