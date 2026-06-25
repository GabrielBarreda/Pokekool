using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class UserCollectionConfiguration : IEntityTypeConfiguration<UserCollection>
{
    public void Configure(EntityTypeBuilder<UserCollection> builder)
    {
        builder.ToTable("user_collection");

        builder.HasKey(uc => uc.Id);
        builder.Property(uc => uc.Id).HasColumnName("id");
        builder.Property(uc => uc.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(uc => uc.CardId).HasColumnName("card_id").IsRequired();
        builder.Property(uc => uc.IsOwned).HasColumnName("is_owned").IsRequired();
        builder.Property(uc => uc.IsFavorite).HasColumnName("is_favorite").IsRequired();
        builder.Property(uc => uc.IsPriority).HasColumnName("is_priority").IsRequired();
        builder.Property(uc => uc.AddedAt).HasColumnName("added_at").IsRequired();
        builder.Property(uc => uc.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(uc => uc.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(uc => new { uc.UserId, uc.CardId })
            .IsUnique()
            .HasDatabaseName("ux_user_collection_user_card");

        builder.HasIndex(uc => uc.UserId)
            .HasDatabaseName("ix_user_collection_user_id");

        builder.HasIndex(uc => uc.CardId)
            .HasDatabaseName("ix_user_collection_card_id");

        builder.HasIndex(uc => new { uc.UserId, uc.IsOwned })
            .HasDatabaseName("ix_user_collection_user_owned");

        builder.HasIndex(uc => new { uc.UserId, uc.IsFavorite })
            .HasDatabaseName("ix_user_collection_user_favorite");

        builder.HasOne(uc => uc.User)
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uc => uc.Card)
            .WithMany()
            .HasForeignKey(uc => uc.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
