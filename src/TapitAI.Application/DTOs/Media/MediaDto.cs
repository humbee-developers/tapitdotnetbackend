using TapitAI.Domain.Enums;

namespace TapitAI.Application.DTOs.Media;

public record MediaDto(
    Guid Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    MediaType MediaType,
    string PublicUrl,
    string? Folder,
    DateTime CreatedAt);

public record UploadMediaDto(
    string FileName,
    string ContentType,
    MediaType MediaType,
    string? Folder);
