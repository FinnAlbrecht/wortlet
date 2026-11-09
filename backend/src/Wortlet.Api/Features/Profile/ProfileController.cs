using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Profiles;
using Wortlet.Domain.Profiles;

namespace Wortlet.Api.Features.Profile;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(ProfileService profiles) : ControllerBase
{
    [HttpGet]
    public Task<ProfileDto> Get(CancellationToken ct) => profiles.GetMineAsync(ct);

    [HttpPut]
    public Task<ProfileDto> Update(UpdateProfileRequest request, CancellationToken ct) => profiles.UpdateAsync(request, ct);

    /// <summary>Symbole und Farben, aus denen man wählen kann.</summary>
    [HttpGet("options")]
    public AvatarOptionsDto Options() => ProfileService.Options;

    /// <summary>Eigenes Bild (multipart, Feld „image“). Der Browser verkleinert es vorher auf 256 px.</summary>
    [HttpPut("avatar")]
    [RequestSizeLimit(AvatarImage.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = AvatarImage.MaxBytes + 64 * 1024)]
    public async Task<ProfileDto> UploadAvatar([FromForm] AvatarForm form, CancellationToken ct)
    {
        if (form.Image is null || form.Image.Length == 0)
            throw new Application.Common.ValidationException("Bitte wähle ein Bild aus.");
        if (form.Image.Length > AvatarImage.MaxBytes)
            throw new Application.Common.ValidationException($"Das Bild ist zu gross (höchstens {AvatarImage.MaxBytes / 1024} KB).");
        using var buffer = new MemoryStream();
        await form.Image.CopyToAsync(buffer, ct);
        return await profiles.UploadImageAsync(buffer.ToArray(), ct);
    }

    [HttpDelete("avatar")]
    public Task<ProfileDto> DeleteAvatar(CancellationToken ct) => profiles.DeleteImageAsync(ct);

    /// <summary>Bild einer Person – nur für sich selbst und Mitglieder derselben Gruppe.</summary>
    [HttpGet("{userId:guid}/avatar")]
    public async Task<IActionResult> Avatar(Guid userId, CancellationToken ct)
    {
        var image = await profiles.GetImageAsync(userId, ct);
        Response.Headers.CacheControl = "private, max-age=3600";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image.Data, image.ContentType);
    }

    public sealed class AvatarForm
    {
        public IFormFile? Image { get; set; }
    }
}
