using System.ComponentModel.DataAnnotations;
using Wortlet.Domain.Languages;

namespace Wortlet.Application.Languages;

public sealed record LanguageDto(Guid Id, string Name, int FolderCount, int WordCount, int WeakCount, DateTime CreatedAt);

public sealed record SaveLanguageRequest(
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(Language.MaxNameLength, ErrorMessage = "Der Name ist zu lang.")]
    string Name);
