using Wortlet.Application.Common;
using Wortlet.Application.Statistics;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Vocabulary;

namespace Wortlet.Application.Quiz;

/// <summary>
/// Prüft Antworten serverseitig (gleiche Logik wie im Original) und führt die Liste
/// „Wörter, die ich nicht kann“. Der Ablauf der Prüfung (Topf, Durchgänge) läuft im Browser.
/// </summary>
public sealed class QuizService(
    IFolderRepository folders,
    IUnitOfWork unitOfWork,
    StatisticsService statistics,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<AnswerResultDto> SubmitAsync(Guid folderId, SubmitAnswerRequest request, CancellationToken ct = default)
    {
        var folder = await Load(folderId, ct);
        var word = Run(() => folder.GetWord(request.WordKey));
        var all = folder.AllWords.ToList();
        var evaluation = AnswerEvaluator.Evaluate(word, all, request.Direction, request.Input, request.Skipped);
        var wasWeak = folder.RecordAnswer(word.Key, evaluation.IsCorrect, request.Round, request.PracticeMode, clock.UtcNow);
        await statistics.RecordAnswerAsync(folder.Id, word.Key, evaluation.IsCorrect, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new AnswerResultDto(evaluation.IsCorrect, evaluation.AcceptedAnswers, evaluation.Solution, wasWeak, folder.WeakWords.Count);
    }

    public async Task<WeakCountDto> OverrideAsync(Guid folderId, OverrideAnswerRequest request, CancellationToken ct = default)
    {
        var folder = await Load(folderId, ct);
        Run(() => { folder.OverrideAsCorrect(request.WordKey, request.Round, request.PracticeMode, request.WasWeakBefore); return 0; });
        await statistics.OverturnAsync(folder.Id, request.WordKey, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new WeakCountDto(folder.WeakWords.Count);
    }

    private static T Run<T>(Func<T> action)
    {
        try { return action(); }
        catch (DomainException ex) { throw new ValidationException(ex.Message); }
    }

    private async Task<Folder> Load(Guid id, CancellationToken ct) =>
        await folders.GetAsync(currentUser.UserId, id, ct)
        ?? throw new NotFoundException("Diesen Ordner gibt es nicht.");
}
