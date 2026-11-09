namespace Wortlet.Domain.Common;

/// <summary>Basis für alle Entitäten mit Identität.</summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

/// <summary>Wurzel eines Aggregats. Nur über sie wird das Aggregat verändert und gespeichert.</summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
}
