namespace TapitAI.Infrastructure.Settings;

public class StorageSettings
{
    public const string SectionName = "Storage";
    public string ActiveProvider { get; init; } = "S3";
    public S3Settings S3 { get; init; } = new();
    public CloudinarySettings Cloudinary { get; init; } = new();
    public GoogleStorageSettings GoogleStorage { get; init; } = new();
}

public class S3Settings
{
    public string AccessKey { get; init; } = null!;
    public string SecretKey { get; init; } = null!;
    public string BucketName { get; init; } = null!;
    public string Region { get; init; } = "us-east-1";
    public string? BaseUrl { get; init; }
}

public class CloudinarySettings
{
    public string CloudName { get; init; } = null!;
    public string ApiKey { get; init; } = null!;
    public string ApiSecret { get; init; } = null!;
}

public class GoogleStorageSettings
{
    public string ProjectId { get; init; } = null!;
    public string BucketName { get; init; } = null!;
    public string? CredentialsPath { get; init; }
}
