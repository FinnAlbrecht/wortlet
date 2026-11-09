using Wortlet.Domain.Folders;

namespace Wortlet.Application.Onboarding;

/// <summary>Startdaten, die jeder neue Benutzer bekommt (Englisch → „Einkaufen &amp; Ernährung“).</summary>
public sealed record StarterContent(
    string LanguageName,
    string FolderName,
    string Description,
    string OldLabel,
    string NewLabel,
    IReadOnlyList<ChapterDraft> Chapters);

public interface IStarterContentProvider
{
    StarterContent Get();
}
