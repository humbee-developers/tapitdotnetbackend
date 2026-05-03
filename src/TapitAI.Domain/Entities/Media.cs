using TapitAI.Domain.Common;
using TapitAI.Domain.Enums;

namespace TapitAI.Domain.Entities;

public class Media : AggregateRoot
{
    public string FileName { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long FileSizeBytes { get; private set; }
    public MediaType MediaType { get; private set; }
    public StorageProviderType StorageProvider { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public string PublicUrl { get; private set; } = null!;
    public string? Folder { get; private set; }
    public string UploadedByUserId { get; private set; } = null!;

    private Media() { }

    public static Media Create(
        string fileName,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        MediaType mediaType,
        StorageProviderType storageProvider,
        string storageKey,
        string publicUrl,
        string uploadedByUserId,
        string? folder = null)
    {
        return new Media
        {
            FileName = fileName,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            MediaType = mediaType,
            StorageProvider = storageProvider,
            StorageKey = storageKey,
            PublicUrl = publicUrl,
            UploadedByUserId = uploadedByUserId,
            Folder = folder
        };
    }
}
