using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Wortlet.Infrastructure.Persistence;

/// <summary>Alle Zeiten sind UTC. SQLite merkt sich die Art nicht, PostgreSQL verlangt sie.</summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc));
