using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Infrastructure.Services.Media.Providers;

public interface IStorageProvider
{
    string ProviderType { get; }

    Task<MediaUploadResult> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        Domain.Enums.MediaType mediaType,
        string? folder,
        CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);
    string GetPublicUrl(string storageKey);
    Task<string> GetSignedUrlAsync(string storageKey, TimeSpan expiry, CancellationToken ct = default);
}
