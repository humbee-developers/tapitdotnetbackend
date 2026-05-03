using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.Infrastructure.Services.Media.Providers;

public class CloudinaryStorageProvider(IOptions<StorageSettings> options) : IStorageProvider
{
    private readonly Cloudinary _cloudinary = new(new Account(
        options.Value.Cloudinary.CloudName,
        options.Value.Cloudinary.ApiKey,
        options.Value.Cloudinary.ApiSecret));

    public string ProviderType => "Cloudinary";

    public async Task<MediaUploadResult> UploadAsync(
        Stream stream, string fileName, string contentType,
        MediaType mediaType, string? folder, CancellationToken ct = default)
    {
        var publicId = $"{Guid.NewGuid():N}_{Path.GetFileNameWithoutExtension(fileName)}";

        if (mediaType == MediaType.Video)
        {
            var videoParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, stream),
                PublicId = string.IsNullOrEmpty(folder) ? publicId : $"{folder}/{publicId}",
                Overwrite = false
            };
            var videoResult = await _cloudinary.UploadAsync(videoParams, ct);
            return new MediaUploadResult(videoResult.PublicId, videoResult.SecureUrl.ToString(), StorageProviderType.Cloudinary);
        }

        var imageParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            PublicId = string.IsNullOrEmpty(folder) ? publicId : $"{folder}/{publicId}",
            Overwrite = false
        };
        var imageResult = await _cloudinary.UploadAsync(imageParams, ct);
        return new MediaUploadResult(imageResult.PublicId, imageResult.SecureUrl.ToString(), StorageProviderType.Cloudinary);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
        => await _cloudinary.DestroyAsync(new DeletionParams(storageKey));

    public string GetPublicUrl(string storageKey)
        => _cloudinary.Api.UrlImgUp.BuildUrl(storageKey);

    public Task<string> GetSignedUrlAsync(string storageKey, TimeSpan expiry, CancellationToken ct = default)
    {
        var url = _cloudinary.Api.UrlImgUp.Signed(true).BuildUrl(storageKey);
        return Task.FromResult(url);
    }
}
