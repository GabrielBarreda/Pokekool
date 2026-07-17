using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities;

namespace Pokekool.Infrastructure.Persistence.Configurations;

public sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_setting");

        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key)
            .HasColumnName("key")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(s => s.Value)
            .HasColumnName("value")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
