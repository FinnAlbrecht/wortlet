using Wortlet.Application.Backup;
using Wortlet.Application.Common;
using Wortlet.Domain.Languages;
using Wortlet.UnitTests.TestDoubles;

namespace Wortlet.UnitTests.Application;

public class BackupServiceTests
{
    private readonly InMemoryLanguages _languages = new();
    private readonly InMemoryFolders _folders = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _service = new BackupService(_languages, _folders, _uow, new FakeCurrentUser(TestData.Owner), new FakeClock());
        var lang = Language.Create(TestData.Owner, "Englisch", TestData.Now);
        _languages.Add(lang);
        var folder = Wortlet.Domain.Folders.Folder.Create(TestData.Owner, lang.Id, "Obst",
            Wortlet.Domain.Vocabulary.VocabularyText.Parse("# Obst | S. 1 | neu\ndie Birne = pear\ndie Kasse = checkout; till | Hinweis").Chapters, TestData.Now);
        folder.MarkWeak("die Birne", TestData.Now);
        _folders.Add(folder);
    }

    [Fact]
    public async Task Export_contains_languages_folders_and_weak_words()
    {
        var doc = await _service.ExportAsync();

        Assert.Equal(BackupDocument.CurrentVersion, doc.Version);
        var lang = Assert.Single(doc.Languages);
        Assert.Equal("Englisch", lang.Name);
        var folder = Assert.Single(lang.Folders);
        Assert.Equal("Obst", folder.Name);
        Assert.Equal(new[] { "die Birne" }, folder.WeakWords);
        Assert.Equal("Hinweis", folder.Chapters[0].Words[1].Hint);
        Assert.True(folder.Chapters[0].IsNew);
    }

    [Fact]
    public async Task Import_replaces_all_data()
    {
        var doc = new BackupDocument(1, "Wortlet", TestData.Now,
        [
            new BackupLanguage("Französisch",
            [
                new BackupFolder("Schule", null, null, null, false, null,
                    [new BackupChapter("Kapitel 1", null, false, [new BackupWord("die Schule", ["l'école"], null), new BackupWord("der Lehrer", ["le professeur"], null)])],
                    ["der Lehrer", "gibt es nicht"]),
            ]),
        ]);

        var result = await _service.ImportAsync(doc);

        Assert.Equal(1, result.Languages);
        Assert.Equal(1, result.Folders);
        Assert.Equal(2, result.Words);
        Assert.Equal(1, result.WeakWords);
        var lang = Assert.Single(_languages.Items);
        Assert.Equal("Französisch", lang.Name);
        var folder = Assert.Single(_folders.Items);
        Assert.Equal(lang.Id, folder.LanguageId);
        Assert.True(folder.IsWeak("der Lehrer"));
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task Export_then_import_round_trips()
    {
        var doc = await _service.ExportAsync();
        await _service.ImportAsync(doc);
        var again = await _service.ExportAsync();

        Assert.Equal(doc.Languages.Count, again.Languages.Count);
        var a = doc.Languages[0].Folders[0];
        var b = again.Languages[0].Folders[0];
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.WeakWords, b.WeakWords);
        Assert.Equal(a.Chapters.SelectMany(c => c.Words).Select(w => w.De), b.Chapters.SelectMany(c => c.Words).Select(w => w.De));
    }

    [Fact]
    public async Task Invalid_file_is_rejected_before_anything_is_deleted()
    {
        var bad = new BackupDocument(1, "Wortlet", TestData.Now,
            [new BackupLanguage("Englisch", [new BackupFolder("Leer", null, null, null, false, null, [], null)])]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.ImportAsync(bad));
        Assert.Contains("Leer", ex.Message);
        Assert.Single(_folders.Items);
        Assert.Equal(0, _uow.Saves);
    }

    [Fact]
    public async Task Wrong_version_is_rejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.ImportAsync(new BackupDocument(99, "Wortlet", TestData.Now, [])));
        await Assert.ThrowsAsync<ValidationException>(() => _service.ImportAsync(null));
    }

    [Fact]
    public async Task Duplicate_language_names_are_rejected()
    {
        var doc = new BackupDocument(1, "Wortlet", TestData.Now, [new BackupLanguage("Englisch", []), new BackupLanguage("englisch", [])]);
        await Assert.ThrowsAsync<ValidationException>(() => _service.ImportAsync(doc));
    }
}
