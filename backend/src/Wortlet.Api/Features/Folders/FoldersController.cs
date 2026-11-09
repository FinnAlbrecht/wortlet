using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Folders;

namespace Wortlet.Api.Features.Folders;

[ApiController]
[Authorize]
[Route("api/folders")]
public sealed class FoldersController(FolderService folders) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public Task<FolderDetailDto> Get(Guid id, CancellationToken ct) => folders.GetAsync(id, ct);

    [HttpPost]
    public Task<FolderDetailDto> Create(CreateFolderRequest request, CancellationToken ct) => folders.CreateAsync(request, ct);

    [HttpPut("{id:guid}")]
    public Task<FolderDetailDto> Update(Guid id, UpdateFolderRequest request, CancellationToken ct) => folders.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await folders.DeleteAsync(id, ct);
        return NoContent();
    }
}
