using Pokekool.Domain.Common;
using Pokekool.Domain.Entities;

namespace Pokekool.Domain.Entities.Pokemon;

public sealed class UserCollection : Entity
{
    public Guid UserId { get; private set; }
    public Guid CardId { get; private set; }
    public bool IsOwned { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool IsPriority { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    // Navigation
    public User? User { get; private set; }
    public PokemonCard? Card { get; private set; }

    private UserCollection() { }

    public static UserCollection Create(Guid userId, Guid cardId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required", nameof(userId));
        if (cardId == Guid.Empty) throw new ArgumentException("CardId is required", nameof(cardId));

        return new UserCollection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardId = cardId,
            IsOwned = true,
            IsFavorite = false,
            IsPriority = false,
            AddedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Creates an entry for a card the user does not own yet (for favorite/priority tracking).</summary>
    public static UserCollection CreateNotOwned(Guid userId, Guid cardId)
    {
        var entry = Create(userId, cardId);
        entry.IsOwned = false;
        return entry;
    }

    public void ToggleOwned()
    {
        IsOwned = !IsOwned;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetFavorite(bool isFavorite)
    {
        IsFavorite = isFavorite;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPriority(bool isPriority)
    {
        if (isPriority && IsOwned)
            throw new DomainException("collection.priority_on_owned", "Cannot mark an owned card as priority.");

        IsPriority = isPriority;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Returns true if this row can be safely deleted (no useful state remaining).</summary>
    public bool CanBeDeleted() => !IsOwned && !IsFavorite && !IsPriority;
}
