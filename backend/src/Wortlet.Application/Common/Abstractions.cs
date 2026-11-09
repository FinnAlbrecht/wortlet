namespace Wortlet.Application.Common;

/// <summary>Der eingeloggte Benutzer der aktuellen Anfrage.</summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}

public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
