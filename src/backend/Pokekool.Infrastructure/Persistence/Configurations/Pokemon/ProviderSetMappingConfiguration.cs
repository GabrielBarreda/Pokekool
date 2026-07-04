using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class ProviderSetMappingConfiguration : IEntityTypeConfiguration<ProviderSetMapping>
{
    public void Configure(EntityTypeBuilder<ProviderSetMapping> builder)
    {
        builder.ToTable("provider_set_mappings");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.SetId).HasColumnName("set_id").IsRequired();
        builder.Property(m => m.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
        builder.Property(m => m.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Value).HasColumnName("value").HasMaxLength(500).IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        // Un seul enregistrement par (set, provider, key)
        builder.HasIndex(m => new { m.SetId, m.Provider, m.Key })
            .IsUnique()
            .HasDatabaseName("ux_provider_set_mappings_set_provider_key");

        // Index provider+key pour les lookups cross-sets (ex: "tous les sets GemTracker")
        builder.HasIndex(m => new { m.Provider, m.Key })
            .HasDatabaseName("ix_provider_set_mappings_provider_key");

        builder.HasOne(m => m.Set)
            .WithMany()
            .HasForeignKey(m => m.SetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
