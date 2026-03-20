using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Search.Infrastructure.Qdrant
{
    public class QdrantSetup
    {
        private readonly QdrantClient _client;

        public QdrantSetup(QdrantClient client)
        {
            _client = client;
        }

        public async Task InitializeAsync()
        {
            await CreateCollectionAsync("products_text");
            await CreateCollectionAsync("products_image");
            Console.WriteLine("Qdrant collections ready.");
        }

        private async Task CreateCollectionAsync(string name)
        {
            var collections = await _client.ListCollectionsAsync();
            if (collections.Any(c => c == name))
            {
                Console.WriteLine($"Collection '{name}' already exists, skipping.");
                return;
            }

            await _client.CreateCollectionAsync(name, new VectorParams
            {
                // CLIP ViT-Large Patch14 models works text/image embedding size: 768
                Size = 768,
                Distance = Distance.Cosine  
            });

            // payload indexes for filtered search
            await _client.CreatePayloadIndexAsync(name, "main_category", PayloadSchemaType.Keyword);
            await _client.CreatePayloadIndexAsync(name, "categories", PayloadSchemaType.Keyword);
            await _client.CreatePayloadIndexAsync(name, "price", PayloadSchemaType.Float);
            await _client.CreatePayloadIndexAsync(name, "average_rating", PayloadSchemaType.Float);
            await _client.CreatePayloadIndexAsync(name, "store", PayloadSchemaType.Keyword);

            Console.WriteLine($"Collection '{name}' created.");
        }
    }
}
