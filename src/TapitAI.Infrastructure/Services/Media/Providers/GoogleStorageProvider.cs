using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.Infrastructure.Services.Media.Providers;

public class GoogleStorageProvider(IOptions<StorageSettings> options) : IStorageProvider
{
    private readonly GoogleStorageSettings _settings = options.Value.GoogleStorage;

    public string ProviderType => "GoogleStorage";

    public async Task<MediaUploadResult> UploadAsync(
        Stream stream, string fileName, string contentType,
        MediaType mediaType, string? folder, CancellationToken ct = default)
    {
        var client = await CreateClientAsync();
        var objectName = BuildObjectName(fileName, folder);

        await client.UploadObjectAsync(
            _settings.BucketName,
            objectName,
            contentType,
            stream,
            cancellationToken: ct);

        var publicUrl = $"https://storage.googleapis.com/{_settings.BucketName}/{objectName}";
        return new MediaUploadResult(objectName, publicUrl, StorageProviderType.GoogleStorage);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var client = await CreateClientAsync();
        await client.DeleteObjectAsync(_settings.BucketName, storageKey, cancellationToken: ct);
    }

    public string GetPublicUrl(string storageKey)
        => $"https://storage.googleapis.com/{_settings.BucketName}/{storageKey}";

    public async Task<string> GetSignedUrlAsync(string storageKey, TimeSpan expiry, CancellationToken ct = default)
    {
        var credential = string.IsNullOrEmpty(_settings.CredentialsPath)
            ? await GoogleCredential.GetApplicationDefaultAsync(ct)
            : GoogleCredential.FromFile(_settings.CredentialsPath);

        var urlSigner = UrlSigner.FromCredential(credential.UnderlyingCredential as ServiceAccountCredential
            ?? throw new InvalidOperationException("Service account credential required for signed URLs."));

        return await urlSigner.SignAsync(_settings.BucketName, storageKey, expiry);
    }

    private async Task<StorageClient> CreateClientAsync()
    {
        if (!string.IsNullOrEmpty(_settings.CredentialsPath))
            return await StorageClient.CreateAsync(GoogleCredential.FromFile(_settings.CredentialsPath));

        return await StorageClient.CreateAsync();
    }

    private static string BuildObjectName(string fileName, string? folder)
    {
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        return string.IsNullOrEmpty(folder) ? safeName : $"{folder.Trim('/')}/{safeName}";
    }
}
