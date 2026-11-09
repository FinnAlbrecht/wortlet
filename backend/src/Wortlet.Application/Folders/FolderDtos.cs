using System.ComponentModel.DataAnnotations;
using Wortlet.Domain.Folders;

namespace Wortlet.Application.Folders;

public sealed record FolderSummaryDto(
    Guid Id, Guid LanguageId, string Name, string Description, bool IsSeed,
    int ChapterCount, int WordCount, int WeakCount, DateTime CreatedAt);

public sealed record WordDto(string Key, string German, IReadOnlyList<string> Answers, string Solution, string? Hint);

public sealed record ChapterDto(string Name, string Pages, bool IsNew, IReadOnlyList<WordDto> Words);

public sealed record FolderDetailDto(
    Guid Id,
    Guid LanguageId,
    string LanguageName,
    string Name,
    string Description,
    bool IsSeed,
    string OldLabel,
    string NewLabel,
    string Text,
    IReadOnlyList<ChapterDto> Chapters,
    IReadOnlyList<string> WeakWords);

public sealed record CreateFolderRequest(
    [Required(ErrorMessage = "Die Sprache fehlt.")] Guid LanguageId,
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(Folder.MaxNameLength, ErrorMessage = "Der Name ist zu lang.")] string Name,
    [Required(ErrorMessage = "Bitte trage Wörter ein.")]
    [StringLength(500_000, ErrorMessage = "Der Text ist zu lang.")] string Text);

public sealed record UpdateFolderRequest(
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(Folder.MaxNameLength, ErrorMessage = "Der Name ist zu lang.")] string Name,
    [Required(ErrorMessage = "Bitte trage Wörter ein.")]
    [StringLength(500_000, ErrorMessage = "Der Text ist zu lang.")] string Text);
