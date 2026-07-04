using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class ProviderCardMappingConfiguration : IEntityTypeConfiguration<ProviderCardMapping>
{
    public void Configure(EntityTypeBuilder<ProviderCardMapping> builder)
    {
        builder.ToTable("provider_card_mappings");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.CardId).HasColumnName("card_id").IsRequired();
        builder.Property(m => m.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
        builder.Property(m => m.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Value).HasColumnName("value").HasMaxLength(500).IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        // Un seul enregistrement par (card, provider, key)
        builder.HasIndex(m => new { m.CardId, m.Provider, m.Key })
            .IsUnique()
            .HasDatabaseName("ux_provider_card_mappings_card_provider_key");

        // Index provider+key pour les batch lookups
        builder.HasIndex(m => new { m.Provider, m.Key })
            .HasDatabaseName("ix_provider_card_mappings_provider_key");

        builder.HasOne(m => m.Card)
            .WithMany()
            .HasForeignKey(m => m.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
