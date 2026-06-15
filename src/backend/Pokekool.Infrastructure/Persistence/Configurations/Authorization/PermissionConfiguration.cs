using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Authorization;

namespace Pokekool.Infrastructure.Persistence.Configurations.Authorization;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Code);
        builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(p => p.Category).HasColumnName("category").HasMaxLength(40).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
    }
}
