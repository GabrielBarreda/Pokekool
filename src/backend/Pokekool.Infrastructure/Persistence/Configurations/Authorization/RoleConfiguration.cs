using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Authorization;

namespace Pokekool.Infrastructure.Persistence.Configurations.Authorization;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(r => r.IsSystem).HasColumnName("is_system").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => new { r.TenantId, r.Code })
            .IsUnique()
            .HasDatabaseName("ux_roles_tenant_code");

        // Garantit l'unicité des codes système (tenant_id IS NULL).
        builder.HasIndex(r => r.Code)
            .IsUnique()
            .HasFilter("tenant_id IS NULL")
            .HasDatabaseName("ux_roles_code_system");

        // Permissions du rôle (table role_permissions)
        builder.OwnsMany(r => r.Permissions, rp =>
        {
            rp.ToTable("role_permissions");
            rp.WithOwner().HasForeignKey(p => p.RoleId);
            rp.Property(p => p.RoleId).HasColumnName("role_id");
            rp.Property(p => p.PermissionCode).HasColumnName("permission_code").HasMaxLength(80).IsRequired();
            rp.HasKey(p => new { p.RoleId, p.PermissionCode });
            rp.HasIndex(p => p.PermissionCode).HasDatabaseName("ix_role_permissions_code");
        });
    }
}
