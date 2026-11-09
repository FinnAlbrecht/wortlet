using Wortlet.Domain.Folders;
using Wortlet.Infrastructure.Seed;

namespace Wortlet.UnitTests.Infrastructure;

public class StarterContentTests
{
    [Fact]
    public void Seed_folder_is_complete()
    {
        var content = new EmbeddedStarterContentProvider().Get();

        Assert.Equal("Englisch", content.LanguageName);
        Assert.Equal("Einkaufen & Ernährung", content.FolderName);
        Assert.Equal(9, content.Chapters.Count);
        Assert.Equal(319, content.Chapters.Sum(c => c.Words.Count));
        Assert.Equal(2, content.Chapters.Count(c => !c.IsNew));
        Assert.Equal("Alte Wörter (S. 201–215)", content.OldLabel);
        Assert.Equal("Neue Wörter (S. 216–225)", content.NewLabel);
    }

    [Fact]
    public void Seed_folder_can_be_created_without_losing_words()
    {
        var content = new EmbeddedStarterContentProvider().Get();
        var folder = Folder.Create(Guid.NewGuid(), Guid.NewGuid(), content.FolderName, content.Chapters, DateTime.UtcNow, content.Description, content.OldLabel, content.NewLabel, true);

        Assert.Equal(319, folder.WordCount);
        Assert.Equal(new[] { "checkout", "cash desk", "till" }, folder.GetWord("die Kasse").Answers);
        Assert.Equal("3 Wörter im Buch, eines reicht", folder.GetWord("die Kasse").Hint);
    }
}
