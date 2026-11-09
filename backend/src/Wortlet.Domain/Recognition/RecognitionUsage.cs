using Wortlet.Domain.Common;

namespace Wortlet.Domain.Recognition;

/// <summary>Wie viele Fotos ein Benutzer an einem Tag erkennen liess. Schützt das gemeinsame OpenRouter-Kontingent.</summary>
public sealed class RecognitionUsage : AggregateRoot<Guid>
{
    public Guid OwnerId { get; private set; }
    public DateOnly Day { get; private set; }
    public int Count { get; private set; }

    private RecognitionUsage() { }

    public static RecognitionUsage Start(Guid ownerId, DateOnly day) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Day = day,
        Count = 0,
    };

    public int Remaining(int dailyLimit) => Math.Max(0, dailyLimit - Count);

    public void Increment() => Count++;
}

public interface IRecognitionUsageRepository
{
    Task<RecognitionUsage?> GetAsync(Guid ownerId, DateOnly day, CancellationToken ct = default);
    void Add(RecognitionUsage usage);
}
