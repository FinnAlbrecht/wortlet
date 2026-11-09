using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Recognition;

namespace Wortlet.Api.Features.Recognition;

[ApiController]
[Authorize]
[Route("api/recognize")]
public sealed class RecognitionController(RecognitionService recognition) : ControllerBase
{
    [HttpGet("status")]
    public Task<RecognitionStatusDto> Status(CancellationToken ct) => recognition.StatusAsync(ct);

    /// <summary>Foto (multipart, Feld „photo“) → erkannte Wörter als Zeilen „Deutsch = Fremdsprache“.</summary>
    [HttpPost]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    public async Task<RecognitionResultDto> Recognize([FromForm] RecognizeForm form, CancellationToken ct)
    {
        if (form.Photo is null || form.Photo.Length == 0)
            throw new RecognitionException(RecognitionFailure.InvalidImage, "Bitte wähle ein Foto aus.");

        using var buffer = new MemoryStream();
        await form.Photo.CopyToAsync(buffer, ct);
        return await recognition.RecognizeAsync(buffer.ToArray(), form.Photo.ContentType, form.Language, ct);
    }

    public sealed class RecognizeForm
    {
        public IFormFile? Photo { get; set; }
        public string? Language { get; set; }
    }
}
