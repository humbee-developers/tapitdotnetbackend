namespace TapitAI.Application.DTOs.Admin;

public class PlaceholderPhotoDto
{
    public Guid Id { get; set; }
    public string Gender { get; set; } = null!;
    public string PhotoUrl { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
