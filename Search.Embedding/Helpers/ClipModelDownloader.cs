namespace Helpers
{
    public static class ClipModelDownloader
    {
        private static readonly HttpClient http = new HttpClient();

        private const string VisionUrl =
            "https://huggingface.co/Xenova/clip-vit-large-patch14/resolve/main/onnx/vision_model.onnx";

        private const string TextUrl =
            "https://huggingface.co/Xenova/clip-vit-large-patch14/resolve/main/onnx/text_model.onnx";

        public static async Task<string> EnsureModelsAsync()
        {
            string modelDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Models");

            Directory.CreateDirectory(modelDir);

            string visionPath = Path.Combine(modelDir, "vision_model.onnx");
            string textPath = Path.Combine(modelDir, "text_model.onnx");

            if (!File.Exists(visionPath))
                await DownloadAsync(VisionUrl, visionPath);

            if (!File.Exists(textPath))
                await DownloadAsync(TextUrl, textPath);

            return modelDir;
        }

        private static async Task DownloadAsync(string url, string destination)
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var file = File.Create(destination);

            await stream.CopyToAsync(file);
        }
    }
}
