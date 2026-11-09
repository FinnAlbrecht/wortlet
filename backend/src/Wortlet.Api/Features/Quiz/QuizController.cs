using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Quiz;

namespace Wortlet.Api.Features.Quiz;

[ApiController]
[Authorize]
[Route("api/folders/{folderId:guid}/quiz")]
public sealed class QuizController(QuizService quiz) : ControllerBase
{
    /// <summary>Antwort prüfen. Falsche Wörter kommen in „Wörter, die ich nicht kann“.</summary>
    [HttpPost("answers")]
    public Task<AnswerResultDto> Submit(Guid folderId, SubmitAnswerRequest request, CancellationToken ct) =>
        quiz.SubmitAsync(folderId, request, ct);

    /// <summary>„War doch richtig“.</summary>
    [HttpPost("override")]
    public Task<WeakCountDto> Override(Guid folderId, OverrideAnswerRequest request, CancellationToken ct) =>
        quiz.OverrideAsync(folderId, request, ct);
}
