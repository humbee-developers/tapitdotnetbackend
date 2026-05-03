using TapitAI.Domain.Interfaces.Services;

namespace TapitAI.Infrastructure.Services.Media;

public class MediaStorageService(MediaStorageFactory factory) : IMediaStorageService
{
    public Task<MediaUploadResult> UploadAsync(MediaUploadRequest request, CancellationToken ct = default)
    {
        var provider = factory.GetActiveProvider();
        return provider.UploadAsync(
            request.Stream,
            request.FileName,
            request.ContentType,
            request.MediaType,
            request.Folder,
            ct);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
        => factory.GetActiveProvider().DeleteAsync(storageKey, ct);

    public Task<string> GetSignedUrlAsync(string storageKey, TimeSpan expiry, CancellationToken ct = default)
        => factory.GetActiveProvider().GetSignedUrlAsync(storageKey, expiry, ct);
}
