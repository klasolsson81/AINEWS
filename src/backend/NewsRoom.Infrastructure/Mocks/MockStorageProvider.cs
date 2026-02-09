using Microsoft.Extensions.Logging;
using NewsRoom.Core.Interfaces;

namespace NewsRoom.Infrastructure.Mocks;

public class MockStorageProvider : IStorageProvider
{
    private readonly ILogger<MockStorageProvider> _logger;
    private readonly Dictionary<string, byte[]> _storage = new();

    public MockStorageProvider(ILogger<MockStorageProvider> logger)
    {
        _logger = logger;
    }

    public Task<string> SaveFileAsync(string category, string fileName, byte[] data, CancellationToken cancellationToken = default)
    {
        var path = $"storage/{category}/{fileName}";
        _storage[path] = data;
        _logger.LogInformation("MockStorageProvider: Saved {Bytes} bytes to {Path}", data.Length, path);
        return Task.FromResult(path);
    }

    public Task<string> SaveFileAsync(string category, string fileName, Stream data, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        data.CopyTo(ms);
        return SaveFileAsync(category, fileName, ms.ToArray(), cancellationToken);
    }

    public Task<byte[]?> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(filePath, out var data);
        return Task.FromResult(data);
    }

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.ContainsKey(filePath));
    }

    public string GetPublicUrl(string filePath) => $"/files/{filePath}";
}
