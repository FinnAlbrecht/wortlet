using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Folders;
using Wortlet.Application.Languages;

namespace Wortlet.Api.Features.Languages;

[ApiController]
[Authorize]
[Route("api/languages")]
public sealed class LanguagesController(LanguageService languages, FolderService folders) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LanguageDto>> List(CancellationToken ct) => languages.ListAsync(ct);

    [HttpGet("{id:guid}")]
    public Task<LanguageDto> Get(Guid id, CancellationToken ct) => languages.GetAsync(id, ct);

    [HttpPost]
    public Task<LanguageDto> Create(SaveLanguageRequest request, CancellationToken ct) => languages.CreateAsync(request, ct);

    [HttpPut("{id:guid}")]
    public Task<LanguageDto> Rename(Guid id, SaveLanguageRequest request, CancellationToken ct) => languages.RenameAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await languages.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/folders")]
    public Task<IReadOnlyList<FolderSummaryDto>> Folders(Guid id, CancellationToken ct) => folders.ListAsync(id, ct);
}
