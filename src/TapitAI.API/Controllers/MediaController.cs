using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.DTOs.Media;
using TapitAI.Application.Features.Media.Commands;
using TapitAI.Domain.Enums;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "AdminOrUser")]
public class MediaController : BaseApiController
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/mpeg", "video/webm",
        "audio/mpeg", "audio/wav",
        "application/pdf"
    ];

    /// <summary>Upload a media file to the configured storage provider.</summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(MediaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(104_857_600)] // 100 MB
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] MediaType mediaType = MediaType.Image,
        [FromQuery] string? folder = null,
        CancellationToken ct = default)
    {
        if (file.Length == 0)
            return BadRequest("File is empty.");

        if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
            return BadRequest($"Content type '{file.ContentType}' is not allowed.");

        await using var stream = file.OpenReadStream();
        var result = await Mediator.Send(new UploadMediaCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            mediaType,
            folder), ct);

        return result.Succeeded
            ? CreatedAtAction(nameof(Upload), result.Data)
            : BadRequest(result.Errors);
    }

    /// <summary>Delete a media file by ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeleteMediaCommand(id), ct);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}
