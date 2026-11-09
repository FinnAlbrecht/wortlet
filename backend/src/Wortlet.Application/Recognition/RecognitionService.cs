using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wortlet.Application.Common;
using Wortlet.Domain.Common;
using Wortlet.Domain.Recognition;

namespace Wortlet.Application.Recognition;

public sealed class RecognitionService(
    IVisionModelClient visionClient,
    IRecognitionUsageRepository usages,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<RecognitionOptions> options,
    ILogger<RecognitionService> logger)
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/gif", "image/heic", "image/heif"];

    public async Task<RecognitionStatusDto> StatusAsync(CancellationToken ct = default)
    {
        var limit = options.Value.DailyLimitPerUser;
        var usage = await usages.GetAsync(currentUser.UserId, clock.Today, ct);
        var used = usage?.Count ?? 0;
        return new RecognitionStatusDto(visionClient.IsConfigured, limit, used, Math.Max(0, limit - used));
    }

    public async Task<RecognitionResultDto> RecognizeAsync(byte[] image, string? mediaType, string? languageName, CancellationToken ct = default)
    {
        if (!visionClient.IsConfigured)
            throw new RecognitionException(RecognitionFailure.NotConfigured,
                "Die Bilderkennung ist auf dem Server nicht eingerichtet (kein OpenRouter-Schlüssel). Nutze „Prompt kopieren“ oder tippe die Wörter ein.");

        var type = (mediaType ?? string.Empty).ToLowerInvariant();
        if (image.Length == 0 || !AllowedTypes.Contains(type))
            throw new RecognitionException(RecognitionFailure.InvalidImage, "Bitte wähle ein Foto (JPEG, PNG oder WebP).");
        if (image.Length > options.Value.MaxImageBytes)
            throw new RecognitionException(RecognitionFailure.InvalidImage,
                $"Das Foto ist zu gross (höchstens {options.Value.MaxImageBytes / 1024 / 1024} MB).");

        var limit = options.Value.DailyLimitPerUser;
        var userId = currentUser.UserId;
        var today = clock.Today;
        var usage = await usages.GetAsync(userId, today, ct);
        if (usage is null)
        {
            usage = RecognitionUsage.Start(userId, today);
            usages.Add(usage);
        }
        if (usage.Remaining(limit) == 0)
            throw new RecognitionException(RecognitionFailure.UserLimitReached,
                $"Du hast heute schon {limit} Fotos erkennen lassen. Das ist das Tageslimit – morgen geht es wieder. Bis dahin hilft „Prompt kopieren“.");

        var completion = await visionClient.CompleteAsync(new VisionRequest(RecognitionPrompt.ForPhoto(languageName ?? "Englisch"), image, type), ct);

        // Die Anfrage hat das Kontingent verbraucht – zählen, auch wenn das Ergebnis unbrauchbar ist.
        usage.Increment();
        await unitOfWork.SaveChangesAsync(ct);

        var chapters = RecognitionResultParser.Parse(completion.Content);
        var lines = RecognitionResultParser.ToLines(chapters);
        var wordCount = lines.Count(l => !l.StartsWith('#'));
        logger.LogInformation("Foto erkannt mit {Model}: {Count} Wörter", completion.Model, wordCount);
        if (wordCount == 0)
            throw new RecognitionException(RecognitionFailure.Unreadable,
                "Auf dem Foto wurden keine Wörter gefunden. Ist die Seite scharf, gerade und gut beleuchtet?");

        return new RecognitionResultDto(chapters, lines, wordCount, completion.Model, usage.Remaining(limit));
    }
}
