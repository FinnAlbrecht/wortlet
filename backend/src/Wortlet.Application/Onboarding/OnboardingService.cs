using Wortlet.Application.Common;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;

namespace Wortlet.Application.Onboarding;

public sealed class OnboardingService(
    IStarterContentProvider starterContent,
    ILanguageRepository languages,
    IFolderRepository folders,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    /// <summary>Legt für einen neuen Benutzer die Sprache „Englisch“ mit dem Ordner aus dem Buch an.</summary>
    public async Task SeedNewUserAsync(Guid userId, CancellationToken ct = default)
    {
        var content = starterContent.Get();
        var now = clock.UtcNow;
        var language = Language.Create(userId, content.LanguageName, now);
        var folder = Folder.Create(
            userId, language.Id, content.FolderName, content.Chapters, now,
            content.Description, content.OldLabel, content.NewLabel, isSeed: true);
        languages.Add(language);
        folders.Add(folder);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
