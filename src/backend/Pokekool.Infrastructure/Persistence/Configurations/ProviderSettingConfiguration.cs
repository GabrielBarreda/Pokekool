using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities;

namespace Pokekool.Infrastructure.Persistence.Configurations;

public sealed class ProviderSettingConfiguration : IEntityTypeConfiguration<ProviderSetting>
{
    public void Configure(EntityTypeBuilder<ProviderSetting> builder)
    {
        builder.ToTable("provider_settings");

        builder.HasKey(s => new { s.Provider, s.Key });

        builder.Property(s => s.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.EncryptedValue)
            .HasColumnName("encrypted_value")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
