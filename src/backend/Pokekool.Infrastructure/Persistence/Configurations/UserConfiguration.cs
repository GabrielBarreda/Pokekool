using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Domain.ValueObjects;

namespace Pokekool.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.TenantId).HasColumnName("tenant_id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(e => e.Value, v => Email.Create(v));

        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        builder.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(120);
        builder.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(120);
        builder.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed").IsRequired();
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email");

        builder.OwnsMany(u => u.RoleAssignments, ra =>
        {
            ra.ToTable("user_roles");
            ra.WithOwner().HasForeignKey(r => r.UserId);

            ra.Property<long>("Id").ValueGeneratedOnAdd().HasColumnName("id");
            ra.HasKey("Id");

            ra.Property(r => r.UserId).HasColumnName("user_id");
            ra.Property(r => r.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();

            ra.HasIndex(r => new { r.UserId, r.RoleId })
                .IsUnique()
                .HasDatabaseName("ux_user_roles_user_role");

            ra.HasOne<Role>()
                .WithMany()
                .HasForeignKey(r => r.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
