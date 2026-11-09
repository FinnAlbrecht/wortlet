using Wortlet.Domain.Common;

namespace Wortlet.Domain.Languages;

/// <summary>Eine Fremdsprache eines Benutzers (z. B. Englisch). Enthält Ordner.</summary>
public sealed class Language : AggregateRoot<Guid>
{
    public const int MaxNameLength = 60;

    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Language() { }

    public static Language Create(Guid ownerId, string name, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Name = Guard.Name(name, MaxNameLength, "Der Name der Sprache"),
        CreatedAt = createdAt,
    };

    public void Rename(string name) => Name = Guard.Name(name, MaxNameLength, "Der Name der Sprache");

    /// <summary>Eine Sprache darf nur gelöscht werden, wenn sie keine Ordner mehr hat.</summary>
    public void EnsureCanBeDeleted(int folderCount)
    {
        if (folderCount > 0)
            throw new DomainException($"Die Sprache „{Name}“ enthält noch {folderCount} Ordner und kann erst gelöscht werden, wenn sie leer ist.");
    }
}
