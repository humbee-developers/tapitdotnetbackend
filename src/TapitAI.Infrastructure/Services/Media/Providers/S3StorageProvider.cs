using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.Infrastructure.Services.Media.Providers;

public class S3StorageProvider(IAmazonS3 s3Client, IOptions<StorageSettings> options) : IStorageProvider
{
    private readonly S3Settings _settings = options.Value.S3;

    public string ProviderType => "S3";

    public async Task<MediaUploadResult> UploadAsync(
        Stream stream, string fileName, string contentType,
        MediaType mediaType, string? folder, CancellationToken ct = default)
    {
        var key = BuildKey(fileName, folder);

        var transferUtility = new TransferUtility(s3Client);
        await transferUtility.UploadAsync(new TransferUtilityUploadRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        }, ct);

        return new MediaUploadResult(key, GetPublicUrl(key), StorageProviderType.S3);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
        => await s3Client.DeleteObjectAsync(_settings.BucketName, storageKey, ct);

    public string GetPublicUrl(string storageKey)
    {
        if (!string.IsNullOrEmpty(_settings.BaseUrl))
            return $"{_settings.BaseUrl.TrimEnd('/')}/{storageKey}";

        return $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{storageKey}";
    }

    public async Task<string> GetSignedUrlAsync(string storageKey, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.Add(expiry)
        };
        return await s3Client.GetPreSignedURLAsync(request);
    }

    private static string BuildKey(string fileName, string? folder)
    {
        var safeFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        return string.IsNullOrEmpty(folder) ? safeFileName : $"{folder.Trim('/')}/{safeFileName}";
    }
}
