using Wortlet.Application.Common;
using Wortlet.Application.Languages;
using Wortlet.Application.Quiz;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Vocabulary;
using Wortlet.UnitTests.TestDoubles;

namespace Wortlet.UnitTests.Application;

public class LanguageAndQuizServiceTests
{
    private readonly InMemoryLanguages _languages = new();
    private readonly InMemoryFolders _folders = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeCurrentUser _user = new(TestData.Owner);
    private readonly FakeClock _clock = new();

    [Fact]
    public async Task Creating_an_existing_language_returns_it()
    {
        var service = new LanguageService(_languages, _folders, _uow, _user, _clock);
        var first = await service.CreateAsync(new SaveLanguageRequest("Französisch"));
        var second = await service.CreateAsync(new SaveLanguageRequest("französisch"));
        Assert.Equal(first.Id, second.Id);
        Assert.Single(_languages.Items);
    }

    [Fact]
    public async Task Deleting_a_non_empty_language_is_a_conflict()
    {
        var service = new LanguageService(_languages, _folders, _uow, _user, _clock);
        var lang = Language.Create(TestData.Owner, "Englisch", TestData.Now);
        _languages.Add(lang);
        _folders.Add(Wortlet.Domain.Folders.Folder.Create(TestData.Owner, lang.Id, "F", VocabularyText.Parse("a = b").Chapters, TestData.Now));

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(lang.Id));
        _folders.Items.Clear();
        await service.DeleteAsync(lang.Id);
        Assert.Empty(_languages.Items);
    }

    [Fact]
    public async Task Other_users_cannot_see_a_language()
    {
        var lang = Language.Create(Guid.NewGuid(), "Spanisch", TestData.Now);
        _languages.Add(lang);
        var service = new LanguageService(_languages, _folders, _uow, _user, _clock);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(lang.Id));
    }

    [Fact]
    public async Task Wrong_answer_goes_to_weak_list_and_override_removes_it()
    {
        var folder = Wortlet.Domain.Folders.Folder.Create(TestData.Owner, TestData.LanguageId, "F",
            VocabularyText.Parse("die Sahne = cream\nder Rahm = cream").Chapters, TestData.Now);
        _folders.Add(folder);
        var quiz = new QuizService(_folders, _uow, _user, _clock);

        var wrong = await quiz.SubmitAsync(folder.Id, new SubmitAnswerRequest("die Sahne", QuizDirection.ToForeign, "creme", false, 1, false));
        Assert.False(wrong.Correct);
        Assert.Equal(1, wrong.WeakCount);
        Assert.False(wrong.WasWeakBefore);

        var undone = await quiz.OverrideAsync(folder.Id, new OverrideAnswerRequest("die Sahne", 1, false, wrong.WasWeakBefore));
        Assert.Equal(0, undone.WeakCount);

        var reverse = await quiz.SubmitAsync(folder.Id, new SubmitAnswerRequest("die Sahne", QuizDirection.ToGerman, "der Rahm", false, 1, false));
        Assert.True(reverse.Correct);
        Assert.Equal("die Sahne / der Rahm", reverse.Solution);
    }
}
