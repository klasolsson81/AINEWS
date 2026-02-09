namespace NewsRoom.Core.Interfaces;

public interface IStorageProvider
{
    Task<string> SaveFileAsync(
        string category,
        string fileName,
        byte[] data,
        CancellationToken cancellationToken = default);

    Task<string> SaveFileAsync(
        string category,
        string fileName,
        Stream data,
        CancellationToken cancellationToken = default);

    Task<byte[]?> GetFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    string GetPublicUrl(string filePath);
}
