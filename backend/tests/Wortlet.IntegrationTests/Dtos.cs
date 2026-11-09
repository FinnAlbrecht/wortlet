namespace Wortlet.IntegrationTests;

// Schlanke Gegenstücke der API-Antworten, damit die Tests das JSON so lesen wie das Frontend.
public sealed record LanguageJson(Guid Id, string Name, int FolderCount, int WordCount, int WeakCount);
public sealed record FolderSummaryJson(Guid Id, Guid LanguageId, string Name, int ChapterCount, int WordCount, int WeakCount, bool IsSeed);
public sealed record WordJson(string Key, string German, List<string> Answers, string Solution, string? Hint);
public sealed record ChapterJson(string Name, string Pages, bool IsNew, List<WordJson> Words);
public sealed record FolderJson(Guid Id, Guid LanguageId, string LanguageName, string Name, string Text, bool IsSeed, string OldLabel, string NewLabel, List<ChapterJson> Chapters, List<string> WeakWords);
public sealed record AnswerJson(bool Correct, List<string> AcceptedAnswers, string Solution, bool WasWeakBefore, int WeakCount);
public sealed record WeakCountJson(int WeakCount);
public sealed record ProblemJson(int Status, string Title, string? Code);
public sealed record RecognitionJson(List<string> Lines, int WordCount, string Model, int RemainingToday);
public sealed record StatusJson(bool Configured, int DailyLimit, int UsedToday, int RemainingToday);
