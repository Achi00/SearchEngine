namespace Search.Application.Dtos.Dataset
{
    public class DatasetLoadResultResponse
    {
        public int Downloaded { get; init; }
        public int Skipped { get; init; }
        public List<string> Failed { get; init; } = [];

        public bool IsSuccess => Failed.Count == 0;
    }
}
