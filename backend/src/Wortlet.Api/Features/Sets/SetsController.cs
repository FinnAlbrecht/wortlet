using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Sets;

namespace Wortlet.Api.Features.Sets;

[ApiController]
[Authorize]
public sealed class SetsController(SetService sets) : ControllerBase
{
    [HttpGet("api/languages/{languageId:guid}/sets")]
    public Task<IReadOnlyList<SetSummaryDto>> List(Guid languageId, CancellationToken ct) => sets.ListAsync(languageId, ct);

    [HttpGet("api/sets/{id:guid}")]
    public Task<SetDetailDto> Get(Guid id, CancellationToken ct) => sets.GetAsync(id, ct);

    [HttpPost("api/sets")]
    public Task<SetDetailDto> Create(CreateSetRequest request, CancellationToken ct) => sets.CreateAsync(request, ct);

    [HttpPut("api/sets/{id:guid}")]
    public Task<SetDetailDto> Update(Guid id, UpdateSetRequest request, CancellationToken ct) => sets.UpdateAsync(id, request, ct);

    [HttpDelete("api/sets/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sets.DeleteAsync(id, ct);
        return NoContent();
    }
}
