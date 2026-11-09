namespace Wortlet.Application.Common;

/// <summary>Basis für Fehler, deren Meldung direkt dem Benutzer gezeigt werden darf.</summary>
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string message) : AppException(message);

public sealed class ConflictException(string message) : AppException(message);

public sealed class ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null) : AppException(message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors ?? new Dictionary<string, string[]>();
}
