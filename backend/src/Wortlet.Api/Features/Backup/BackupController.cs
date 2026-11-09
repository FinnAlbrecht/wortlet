using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wortlet.Application.Backup;

namespace Wortlet.Api.Features.Backup;

[ApiController]
[Authorize]
[Route("api/backup")]
public sealed class BackupController(BackupService backup) : ControllerBase
{
    [HttpGet("export")]
    public Task<BackupDocument> Export(CancellationToken ct) => backup.ExportAsync(ct);

    /// <summary>Überschreibt alle Daten des Benutzers. Das Frontend fragt vorher nach.</summary>
    [HttpPost("import")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public Task<ImportResultDto> Import(BackupDocument document, CancellationToken ct) => backup.ImportAsync(document, ct);
}
