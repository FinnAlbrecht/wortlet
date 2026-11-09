using Wortlet.Domain.Vocabulary;
using Wortlet.UnitTests.TestDoubles;

namespace Wortlet.UnitTests.Domain;

public class VocabularyTextTests
{
    [Fact]
    public void Parses_lines_alternatives_and_chapters()
    {
        var result = VocabularyText.Parse("# Obst\ndie Birne = pear\ndie Kasse = checkout; till\n\n# Gemüse\ndie Gurke = cucumber");

        Assert.Equal(2, result.Chapters.Count);
        Assert.Equal("Obst", result.Chapters[0].Name);
        Assert.Equal(new[] { "checkout", "till" }, result.Chapters[0].Words[1].Answers);
        Assert.Equal(3, result.WordCount);
        Assert.Empty(result.InvalidLines);
    }

    [Fact]
    public void Words_without_chapter_go_to_default_chapter_and_invalid_lines_are_reported()
    {
        var result = VocabularyText.Parse("die Birne = pear\nkein Gleichheitszeichen\n = leer\ndie Gurke =");

        Assert.Single(result.Chapters);
        Assert.Equal(VocabularyText.DefaultChapterName, result.Chapters[0].Name);
        Assert.Equal(new[] { 2, 3, 4 }, result.InvalidLines);
    }

    [Fact]
    public void Empty_chapters_are_dropped()
    {
        var result = VocabularyText.Parse("# Leer\n# Voll\ndie Birne = pear");
        Assert.Single(result.Chapters);
        Assert.Equal("Voll", result.Chapters[0].Name);
    }

    [Fact]
    public void Optional_pages_new_marker_and_hint()
    {
        var result = VocabularyText.Parse("# Obst | S. 216–217 | neu\ndie Kasse = checkout; till | eines reicht");
        var chapter = result.Chapters[0];
        Assert.Equal("Obst", chapter.Name);
        Assert.Equal("S. 216–217", chapter.Pages);
        Assert.True(chapter.IsNew);
        Assert.Equal("eines reicht", chapter.Words[0].Hint);
        Assert.Equal(new[] { "checkout", "till" }, chapter.Words[0].Answers);
    }

    [Fact]
    public void Duplicate_german_entries_get_a_suffix()
    {
        var folder = TestData.Folder("die Bank = bank\ndie Bank = bench");
        Assert.NotNull(folder.FindWord("die Bank"));
        Assert.NotNull(folder.FindWord("die Bank (2)"));
    }

    [Fact]
    public void Format_and_parse_round_trip()
    {
        const string text = "# Obst | S. 216–217 | neu\ndie Kasse = checkout; till | eines reicht\ndie Bank = bank\ndie Bank = bench\n\n# Alt\nder Preis = price";
        var folder = TestData.Folder(text);
        var formatted = VocabularyText.Format(folder.Chapters);
        var again = TestData.Folder(formatted);

        Assert.Equal(formatted, VocabularyText.Format(again.Chapters));
        Assert.Equal(folder.AllWords.Select(w => w.Key), again.AllWords.Select(w => w.Key));
        Assert.True(again.Chapters[0].IsNew);
        Assert.Equal("eines reicht", again.GetWord("die Kasse").Hint);
    }
}
