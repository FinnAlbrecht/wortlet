using System.Text.Json;
using Wortlet.Application.Onboarding;
using Wortlet.Domain.Folders;

namespace Wortlet.Infrastructure.Seed;

/// <summary>Liest den Ordner „Einkaufen &amp; Ernährung“ (aus SETS der HTML-Version) aus der eingebetteten JSON-Datei.</summary>
public sealed class EmbeddedStarterContentProvider : IStarterContentProvider
{
    private static readonly Lazy<StarterContent> Content = new(Load);

    public StarterContent Get() => Content.Value;

    private static StarterContent Load()
    {
        using var stream = typeof(EmbeddedStarterContentProvider).Assembly.GetManifestResourceStream("Wortlet.StarterContent.json")
            ?? throw new InvalidOperationException("Startdaten (Wortlet.StarterContent.json) fehlen.");
        var file = JsonSerializer.Deserialize<SeedFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Startdaten sind leer.");

        return new StarterContent(
            file.Language.Name,
            file.Folder.Name,
            file.Folder.Description,
            file.Folder.OldLabel,
            file.Folder.NewLabel,
            file.Folder.Chapters
                .Select(c => new ChapterDraft(c.Name, c.Pages, c.IsNew, c.Words.Select(w => new WordDraft(w.De, w.Answers, w.Hint)).ToList()))
                .ToList());
    }

    private sealed record SeedFile(SeedLanguage Language, SeedFolder Folder);
    private sealed record SeedLanguage(string Name, string Code);
    private sealed record SeedFolder(string Name, string Description, string OldLabel, string NewLabel, List<SeedChapter> Chapters);
    private sealed record SeedChapter(string Name, string Pages, bool IsNew, List<SeedWord> Words);
    private sealed record SeedWord(string De, List<string> Answers, string? Hint);
}
