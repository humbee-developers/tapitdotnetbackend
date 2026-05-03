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

    [HttpPost]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand cmd, CancellationToken ct)
        => Ok(await Mediator.Send(cmd, ct));

    [HttpPost("photos")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadPhotos([FromForm] IFormFileCollection photos, CancellationToken ct)
        => Ok(await Mediator.Send(new UploadPhotosCommand(photos.ToList()), ct));

    [HttpDelete("photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid photoId, CancellationToken ct)
        => Ok(await Mediator.Send(new DeletePhotoCommand(photoId), ct));

    [HttpPut("photos/{photoId:guid}/primary")]
    public async Task<IActionResult> SetPrimaryPhoto(Guid photoId, CancellationToken ct)
        => Ok(await Mediator.Send(new SetPrimaryPhotoCommand(photoId), ct));

    [HttpPost("videos")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> UploadVideo([FromForm] IFormFile video, CancellationToken ct)
        => Ok(await Mediator.Send(new UploadVideoCommand(video), ct));

    [HttpDelete("videos/{videoId:guid}")]
    public async Task<IActionResult> DeleteVideo(Guid videoId, CancellationToken ct)
        => Ok(await Mediator.Send(new DeleteVideoCommand(videoId), ct));
}
