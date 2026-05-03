using TapitAI.Domain.Enums;

namespace TapitAI.Domain.Interfaces.Services;

public record MediaUploadRequest(
    Stream Stream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    MediaType MediaType,
    string? Folder = null);

public record MediaUploadResult(
    string StorageKey,
    string PublicUrl,
    StorageProviderType Provider);

public interface IMediaStorageService
{
    Task<MediaUploadResult> UploadAsync(MediaUploadRequest request, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
    Task<string> GetSignedUrlAsync(string storageKey, TimeSpan expiry, CancellationToken ct = default);
}
