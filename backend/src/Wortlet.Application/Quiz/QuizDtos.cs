using System.ComponentModel.DataAnnotations;
using Wortlet.Domain.Vocabulary;

namespace Wortlet.Application.Quiz;

public sealed record SubmitAnswerRequest(
    [Required] string WordKey,
    QuizDirection Direction,
    [StringLength(500)] string? Input,
    bool Skipped,
    [Range(1, 10_000)] int Round,
    bool PracticeMode);

public sealed record AnswerResultDto(
    bool Correct,
    IReadOnlyList<string> AcceptedAnswers,
    string Solution,
    bool WasWeakBefore,
    int WeakCount);

public sealed record OverrideAnswerRequest(
    [Required] string WordKey,
    [Range(1, 10_000)] int Round,
    bool PracticeMode,
    bool WasWeakBefore);

public sealed record WeakCountDto(int WeakCount);
