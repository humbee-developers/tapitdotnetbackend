using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.DatingProfile.Commands;
using TapitAI.Application.Features.DatingProfile.Queries;
using TapitAI.Application.DTOs.Dating;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class DatingProfileController : BaseApiController
{
    [HttpGet]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        => Ok(await Mediator.Send(new GetMyProfileQuery(), ct));

    [HttpPut]
    public async Task<IActionResult> UpsertProfile([FromBody] UpsertProfileCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPatch("basic-info")]
    public async Task<IActionResult> UpdateBasicInfo([FromBody] UpdateBasicInfoCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPatch("lifestyle")]
    public async Task<IActionResult> UpdateLifestyle([FromBody] UpdateLifestyleBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateLifestyleCommand(body.Lifestyle), ct));

    [HttpPatch("looking-for")]
    public async Task<IActionResult> UpdateLookingFor([FromBody] UpdateLookingForBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateLookingForCommand(body.LookingFor), ct));

    [HttpPatch("bio")]
    public async Task<IActionResult> UpdateBio([FromBody] UpdateBioBody body, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateBioCommand(body.Bio), ct));

    [HttpPost("photos")]
    [RequestSizeLimit(50_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPhotos([FromForm] UploadPhotosRequest request, CancellationToken ct)
        => Ok(await Mediator.Send(new UploadPhotosCommand(request.Photos), ct));

    [HttpDelete("photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid photoId, CancellationToken ct)
        => Ok(await Mediator.Send(new DeletePhotoCommand(photoId), ct));

    [HttpPut("photos/{photoId:guid}/primary")]
    public async Task<IActionResult> SetPrimaryPhoto(Guid photoId, CancellationToken ct)
        => Ok(await Mediator.Send(new SetPrimaryPhotoCommand(photoId), ct));

    [HttpPost("videos")]
    [RequestSizeLimit(200_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadVideo([FromForm] UploadVideoRequest request, CancellationToken ct)
        => Ok(await Mediator.Send(new UploadVideoCommand(request.Video), ct));

    [HttpDelete("videos/{videoId:guid}")]
    public async Task<IActionResult> DeleteVideo(Guid videoId, CancellationToken ct)
        => Ok(await Mediator.Send(new DeleteVideoCommand(videoId), ct));
}

public class UploadPhotosRequest
{
    public List<IFormFile> Photos { get; set; } = [];
}

public class UploadVideoRequest
{
    public IFormFile Video { get; set; } = null!;
}

public record UpdateLookingForBody(string[] LookingFor);
public record UpdateLifestyleBody(string[] Lifestyle);
public record UpdateBioBody(string? Bio);
