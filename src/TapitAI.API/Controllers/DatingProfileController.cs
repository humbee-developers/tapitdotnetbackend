using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TapitAI.Application.Features.DatingProfile.Commands;
using TapitAI.Application.Features.DatingProfile.Queries;

namespace TapitAI.API.Controllers;

[Authorize(Policy = "UserOnly")]
public class DatingProfileController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        => Ok(await Mediator.Send(new GetMyProfileQuery(), ct));

    [HttpPut]
    public async Task<IActionResult> UpsertProfile([FromBody] UpsertProfileCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

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
