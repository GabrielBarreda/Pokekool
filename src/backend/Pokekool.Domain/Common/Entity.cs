namespace Pokekool.Domain.Common;

/// <summary>
/// Classe de base pour toutes les entités du domaine.
/// Identité = Id, égalité référentielle.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }

    public override bool Equals(object? obj)
        => obj is Entity other && Id == other.Id && GetType() == other.GetType();

    public override int GetHashCode() => HashCode.Combine(Id, GetType());

    public static bool operator ==(Entity? a, Entity? b)
        => a is null ? b is null : a.Equals(b);

    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}
