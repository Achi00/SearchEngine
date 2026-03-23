namespace Search.Application.Interfaces.ML.BackgroundRemoval
{
    public interface IBGRemovalService
    {
        Task<byte[]> RemoveBackgroundAsync(byte[] imageBytes, CancellationToken ct = default);
    }
}