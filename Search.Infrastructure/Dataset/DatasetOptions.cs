namespace Search.Infrastructure.Dataset
{
    public class DatasetOptions
    {
        public const string SectionName = "Dataset";

        /// <summary>
        /// Absolute path to the folder containing dataset files.
        /// Configure this per environment in appsettings.
        /// </summary>
        public string DatasetPath { get; set; } = string.Empty;
    }
}
