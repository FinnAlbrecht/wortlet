using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Statistics;

namespace Wortlet.Api.Features.Statistics;

[ApiController]
[Authorize]
[Route("api/stats")]
public sealed class StatisticsController(StatisticsService statistics) : ControllerBase
{
    [HttpGet("overview")]
    public Task<StatsOverviewDto> Overview(CancellationToken ct) => statistics.OverviewAsync(ct);

    [HttpGet("results")]
    public Task<IReadOnlyList<QuizResultDto>> Results([FromQuery] int take = 50, CancellationToken ct = default) =>
        statistics.ResultsAsync(take, ct);

    /// <summary>Wörter mit der höchsten Fehlerquote, optional nur einer Sprache.</summary>
    [HttpGet("hardest")]
    public Task<IReadOnlyList<HardWordDto>> Hardest([FromQuery] Guid? languageId, [FromQuery] int take = 10, CancellationToken ct = default) =>
        statistics.HardestAsync(languageId, take, ct);

    /// <summary>Ergebnis nach dem ersten Durchgang einer Prüfung speichern.</summary>
    [HttpPost("results")]
    public Task<QuizResultDto> SaveResult(SaveQuizResultRequest request, CancellationToken ct) =>
        statistics.SaveResultAsync(request, ct);
}
