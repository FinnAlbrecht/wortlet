using Wortlet.Domain.Folders;
using Wortlet.Domain.Vocabulary;

namespace Wortlet.UnitTests.TestDoubles;

internal static class TestData
{
    public static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid LanguageId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    public static Folder Folder(string text, string name = "Test") =>
        Wortlet.Domain.Folders.Folder.Create(Owner, LanguageId, name, VocabularyText.Parse(text).Chapters, Now);
}
