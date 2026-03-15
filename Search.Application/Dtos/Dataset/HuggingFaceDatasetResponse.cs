namespace Search.Application.Dtos.Dataset
{
    public class HuggingFaceDatasetResponse
    {
        public string type { get; set; }
        public string oid { get; set; }
        public int size { get; set; }
        public string path { get; set; }
        public string xetHash { get; set; }
        public Lfs lfs { get; set; }
    }

    public class Lfs
    {
        public string oid { get; set; }
        public long size { get; set; }
        public int pointerSize { get; set; }
    }
}
