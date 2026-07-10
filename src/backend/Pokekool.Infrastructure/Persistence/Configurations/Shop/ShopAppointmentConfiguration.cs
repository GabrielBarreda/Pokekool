using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Infrastructure.Persistence.Configurations.Shop;

public sealed class ShopAppointmentConfiguration : IEntityTypeConfiguration<ShopAppointment>
{
    public void Configure(EntityTypeBuilder<ShopAppointment> builder)
    {
        builder.ToTable("shop_appointments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.ShopId).HasColumnName("shop_id").IsRequired();
        builder.Property(a => a.ClientName).HasColumnName("client_name").HasMaxLength(200).IsRequired();
        builder.Property(a => a.ClientEmail).HasColumnName("client_email").HasMaxLength(255).IsRequired();
        builder.Property(a => a.ClientPhone).HasColumnName("client_phone").HasMaxLength(40);
        builder.Property(a => a.ServiceType).HasColumnName("service_type")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.ScheduledAt).HasColumnName("scheduled_at").IsRequired();
        builder.Property(a => a.DurationMinutes).HasColumnName("duration_minutes").IsRequired();
        builder.Property(a => a.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.NotesClient).HasColumnName("notes_client");
        builder.Property(a => a.NotesInternal).HasColumnName("notes_internal");
        builder.Property(a => a.AssignedToUserId).HasColumnName("assigned_to_user_id");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(a => a.ShopId).HasDatabaseName("ix_shop_appointments_shop_id");
        builder.HasIndex(a => a.ScheduledAt).HasDatabaseName("ix_shop_appointments_scheduled_at");
        builder.HasIndex(a => a.Status).HasDatabaseName("ix_shop_appointments_status");

        builder.HasOne<Domain.Entities.Shop.Shop>()
            .WithMany()
            .HasForeignKey(a => a.ShopId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
