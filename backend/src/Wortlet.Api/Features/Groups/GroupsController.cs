using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wortlet.Api.Features.Auth;
using Wortlet.Application.Groups;

namespace Wortlet.Api.Features.Groups;

[ApiController]
[Authorize]
[Route("api/groups")]
public sealed class GroupsController(GroupService groups) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<GroupSummaryDto>> List(CancellationToken ct) => groups.ListMineAsync(ct);

    [HttpPost]
    public Task<GroupDetailDto> Create(CreateGroupRequest request, CancellationToken ct) => groups.CreateAsync(request, ct);

    /// <summary>Öffentliche Gruppen und Gruppen „auf Anfrage“ nach Namen suchen.</summary>
    [HttpGet("search")]
    public Task<IReadOnlyList<GroupSearchResultDto>> Search([FromQuery] string? q, CancellationToken ct) => groups.SearchAsync(q, ct);

    /// <summary>Eigene offene Beitrittsanfragen.</summary>
    [HttpGet("requests")]
    public Task<IReadOnlyList<MyRequestDto>> MyRequests(CancellationToken ct) => groups.MyRequestsAsync(ct);

    /// <summary>Aus der Suche beitreten: öffentlich sofort, sonst als Anfrage.</summary>
    [HttpPost("{id:guid}/join")]
    public Task<JoinResultDto> JoinFromSearch(Guid id, CancellationToken ct) => groups.JoinFromSearchAsync(id, ct);

    [HttpDelete("{id:guid}/request")]
    public async Task<IActionResult> CancelRequest(Guid id, CancellationToken ct)
    {
        await groups.CancelRequestAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/requests/{userId:guid}/accept")]
    public Task<GroupDetailDto> AcceptRequest(Guid id, Guid userId, CancellationToken ct) => groups.AcceptRequestAsync(id, userId, ct);

    [HttpDelete("{id:guid}/requests/{userId:guid}")]
    public Task<GroupDetailDto> DeclineRequest(Guid id, Guid userId, CancellationToken ct) => groups.DeclineRequestAsync(id, userId, ct);

    [HttpPut("{id:guid}/visibility")]
    public Task<GroupDetailDto> Visibility(Guid id, VisibilityRequest request, CancellationToken ct) => groups.ChangeVisibilityAsync(id, request, ct);

    /// <summary>Beitreten per Code. Begrenzt wie die Anmeldung, damit niemand Codes durchprobieren kann.</summary>
    [HttpPost("join")]
    [EnableRateLimiting(RateLimits.Auth)]
    public Task<GroupDetailDto> Join(JoinGroupRequest request, CancellationToken ct) => groups.JoinAsync(request, ct);

    [HttpGet("{id:guid}")]
    public Task<GroupDetailDto> Get(Guid id, CancellationToken ct) => groups.GetAsync(id, ct);

    [HttpPut("{id:guid}")]
    public Task<GroupDetailDto> Rename(Guid id, GroupNameRequest request, CancellationToken ct) => groups.RenameAsync(id, request, ct);

    [HttpPost("{id:guid}/code")]
    public Task<GroupDetailDto> NewCode(Guid id, CancellationToken ct) => groups.NewCodeAsync(id, ct);

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        await groups.LeaveAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await groups.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public Task<GroupDetailDto> RemoveMember(Guid id, Guid userId, CancellationToken ct) => groups.RemoveMemberAsync(id, userId, ct);

    [HttpPost("{id:guid}/shares")]
    public Task<GroupDetailDto> Share(Guid id, ShareRequest request, CancellationToken ct) => groups.ShareAsync(id, request, ct);

    [HttpDelete("{id:guid}/shares/{shareId:guid}")]
    public Task<GroupDetailDto> Unshare(Guid id, Guid shareId, CancellationToken ct) => groups.UnshareAsync(id, shareId, ct);

    /// <summary>Geteilten Ordner bzw. geteiltes Set als eigenen Ordner übernehmen (oder die Kopie aktualisieren).</summary>
    [HttpPost("{id:guid}/shares/{shareId:guid}/copy")]
    public Task<CopyResultDto> Copy(Guid id, Guid shareId, CancellationToken ct) => groups.CopyShareAsync(id, shareId, ct);
}
