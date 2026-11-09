namespace Wortlet.Domain.Common;

/// <summary>Eine fachliche Regel wurde verletzt. Die Meldung ist für Benutzer gedacht (Deutsch).</summary>
public class DomainException(string message) : Exception(message);
