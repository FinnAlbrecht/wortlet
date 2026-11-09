namespace Wortlet.Application.Recognition;

public sealed record VisionRequest(string Prompt, byte[] Image, string MediaType);

public sealed record VisionCompletion(string Content, string Model);

/// <summary>Ein KI-Modell, das Bilder lesen kann (umgesetzt mit OpenRouter in der Infrastruktur).</summary>
public interface IVisionModelClient
{
    bool IsConfigured { get; }
    Task<VisionCompletion> CompleteAsync(VisionRequest request, CancellationToken ct = default);
}

public enum RecognitionFailure
{
    NotConfigured,
    InvalidImage,
    UserLimitReached,
    ProviderQuotaExhausted,
    ModelsUnavailable,
    Unreadable,
    ProviderError,
}

public sealed class RecognitionException(RecognitionFailure failure, string message) : Exception(message)
{
    public RecognitionFailure Failure { get; } = failure;
}

public sealed class RecognitionOptions
{
    public const string SectionName = "Recognition";

    /// <summary>Fotos pro Benutzer und Tag. Das Gratis-Kontingent von OpenRouter gilt fürs ganze Konto.</summary>
    public int DailyLimitPerUser { get; set; } = 30;

    public long MaxImageBytes { get; set; } = 10 * 1024 * 1024;
}

public sealed record RecognitionStatusDto(bool Configured, int DailyLimit, int UsedToday, int RemainingToday);

public sealed record RecognizedWordDto(string De, IReadOnlyList<string> Answers);

public sealed record RecognizedChapterDto(string Name, IReadOnlyList<RecognizedWordDto> Words);

public sealed record RecognitionResultDto(
    IReadOnlyList<RecognizedChapterDto> Chapters,
    IReadOnlyList<string> Lines,
    int WordCount,
    string Model,
    int RemainingToday);
