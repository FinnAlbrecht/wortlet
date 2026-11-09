using System.ComponentModel.DataAnnotations;
using Wortlet.Application.Common;
using ValidationException = Wortlet.Application.Common.ValidationException;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Sets;
using Wortlet.Domain.Statistics;
using Wortlet.Domain.Vocabulary;

namespace Wortlet.Application.Statistics;

public sealed record DayDto(DateOnly Day, int Answers, int Correct);

public sealed record StatsOverviewDto(
    int TotalAnswers,
    int CorrectAnswers,
    int WordsPracticed,
    int Quizzes,
    double? AverageGrade,
    int CurrentStreak,
    int LongestStreak,
    int TodayAnswers,
    int WeekAnswers,
    IReadOnlyList<DayDto> LastDays);

public sealed record QuizResultDto(
    Guid Id, Guid? FolderId, Guid? SetId, string Title, string Direction, bool PracticeMode,
    int WordCount, int FirstRoundCorrect, double Grade, DateTime FinishedAt);

public sealed record HardWordDto(Guid FolderId, string FolderName, string German, string Solution, int Attempts, int Wrong);

public sealed record SaveQuizResultRequest(
    Guid? FolderId,
    Guid? SetId,
    [Required] string Direction,
    bool PracticeMode,
    [Range(1, 10_000)] int WordCount,
    [Range(0, 10_000)] int FirstRoundCorrect);

/// <summary>Tage werden in Schweizer Zeit gezählt, damit „heute geübt“ um Mitternacht wechselt und nicht um 1 bzw. 2 Uhr.</summary>
public static class SwissCalendar
{
    private static readonly TimeZoneInfo Zone = Find();

    private static TimeZoneInfo Find()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    public static DateOnly Day(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));
}

public sealed class StatisticsService(
    IStatisticsRepository stats,
    IFolderRepository folders,
    IWordSetRepository sets,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public const int LastDaysShown = 28;

    /// <summary>Zählt eine Antwort pro Wort und pro Tag. Speichert nicht – das macht der Aufrufer.</summary>
    public async Task RecordAnswerAsync(Guid folderId, string wordKey, bool correct, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var now = clock.UtcNow;

        var stat = await stats.GetWordStatAsync(userId, folderId, wordKey, ct);
        if (stat is null)
        {
            stat = WordStat.Start(userId, folderId, wordKey);
            stats.Add(stat);
        }
        stat.Record(correct, now);

        var day = await GetOrStartDay(userId, SwissCalendar.Day(now), ct);
        day.Record(correct);
    }

    /// <summary>„War doch richtig“ nachträglich in der Statistik berücksichtigen. Speichert nicht.</summary>
    public async Task OverturnAsync(Guid folderId, string wordKey, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        (await stats.GetWordStatAsync(userId, folderId, wordKey, ct))?.Overturn();
        (await stats.GetDayAsync(userId, SwissCalendar.Day(clock.UtcNow), ct))?.Overturn();
    }

    public async Task<QuizResultDto> SaveResultAsync(SaveQuizResultRequest request, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        string title;
        if (request.FolderId is Guid folderId)
        {
            var folder = await folders.GetAsync(userId, folderId, ct) ?? throw new NotFoundException("Diesen Ordner gibt es nicht.");
            title = folder.Name;
        }
        else if (request.SetId is Guid setId)
        {
            var set = await sets.GetAsync(userId, setId, ct) ?? throw new NotFoundException("Dieses Set gibt es nicht.");
            title = set.Name;
        }
        else
        {
            throw new ValidationException("Ordner oder Set fehlt.");
        }

        if (request.PracticeMode) title = "Nicht gekonnt · " + title;
        if (title.Length > QuizResult.MaxTitleLength) title = title[..QuizResult.MaxTitleLength];

        QuizResult result;
        try
        {
            result = QuizResult.Create(userId, request.FolderId, request.SetId, title, request.Direction,
                request.PracticeMode, request.WordCount, request.FirstRoundCorrect, clock.UtcNow);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }
        stats.Add(result);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(result);
    }

    public async Task<StatsOverviewDto> OverviewAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var today = SwissCalendar.Day(clock.UtcNow);
        var days = await stats.ListDaysAsync(userId, ct);
        var totals = await stats.TotalsAsync(userId, ct);
        var (current, longest) = Streaks.Compute(days.Where(d => d.Answers > 0).Select(d => d.Day), today);
        var weekStart = Streaks.WeekStart(today);
        var byDay = days.ToDictionary(d => d.Day);

        var last = Enumerable.Range(0, LastDaysShown)
            .Select(k => today.AddDays(k - LastDaysShown + 1))
            .Select(d => byDay.TryGetValue(d, out var p) ? new DayDto(d, p.Answers, p.Correct) : new DayDto(d, 0, 0))
            .ToList();

        return new StatsOverviewDto(
            days.Sum(d => d.Answers),
            days.Sum(d => d.Correct),
            totals.WordsPracticed,
            totals.Quizzes,
            totals.AverageGrade is double g ? Math.Round(g, 2) : null,
            current,
            longest,
            byDay.TryGetValue(today, out var t) ? t.Answers : 0,
            days.Where(d => d.Day >= weekStart && d.Day <= today).Sum(d => d.Answers),
            last);
    }

    public async Task<IReadOnlyList<QuizResultDto>> ResultsAsync(int take, CancellationToken ct = default) =>
        (await stats.ListResultsAsync(currentUser.UserId, Math.Clamp(take, 1, 200), ct)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<HardWordDto>> HardestAsync(Guid? languageId, int take, CancellationToken ct = default)
    {
        var list = await stats.HardestAsync(currentUser.UserId, languageId, Math.Clamp(take, 1, 100), ct);
        var result = new List<HardWordDto>();
        foreach (var group in list.GroupBy(h => h.FolderId))
        {
            var folder = await folders.GetAsync(currentUser.UserId, group.Key, ct);
            if (folder is null) continue;
            foreach (var h in group)
            {
                if (folder.FindWord(h.WordKey) is not { } word) continue; // Wort wurde inzwischen geändert/gelöscht
                result.Add(new HardWordDto(h.FolderId, folder.Name, AnswerEvaluator.BaseKey(word.Key),
                    AnswerEvaluator.Solution(word.Answers), h.Attempts, h.Wrong));
            }
        }
        return result
            .OrderByDescending(h => (double)h.Wrong / h.Attempts)
            .ThenByDescending(h => h.Wrong)
            .ToList();
    }

    private async Task<PracticeDay> GetOrStartDay(Guid userId, DateOnly day, CancellationToken ct)
    {
        var existing = await stats.GetDayAsync(userId, day, ct);
        if (existing is not null) return existing;
        var created = PracticeDay.Start(userId, day);
        stats.Add(created);
        return created;
    }

    private static QuizResultDto ToDto(QuizResult r) =>
        new(r.Id, r.FolderId, r.SetId, r.Title, r.Direction, r.PracticeMode, r.WordCount, r.FirstRoundCorrect, r.Grade, r.FinishedAt);
}
