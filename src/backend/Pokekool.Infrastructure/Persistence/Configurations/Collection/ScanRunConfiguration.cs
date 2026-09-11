using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Collection;

namespace Pokekool.Infrastructure.Persistence.Configurations.Collection;

public sealed class ScanRunConfiguration : IEntityTypeConfiguration<ScanRun>
{
    public void Configure(EntityTypeBuilder<ScanRun> builder)
    {
        builder.ToTable("scan_runs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.RunAt).HasColumnName("run_at").IsRequired();
        builder.Property(r => r.DestinationType).HasColumnName("destination_type")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.AddedToStockDirectly).HasColumnName("added_to_stock_directly");
        builder.Property(r => r.ThirdPartyName).HasColumnName("third_party_name").HasMaxLength(200);
        builder.Property(r => r.Reason).HasColumnName("reason")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ReasonOtherText).HasColumnName("reason_other_text").HasMaxLength(500);

        builder.Property(r => r.TotalPhotos).HasColumnName("total_photos").IsRequired();
        builder.Property(r => r.Succeeded).HasColumnName("succeeded").IsRequired();
        builder.Property(r => r.Failed).HasColumnName("failed").IsRequired();
        builder.Property(r => r.AverageDistance).HasColumnName("average_distance");
        builder.Property(r => r.AverageElapsedMs).HasColumnName("average_elapsed_ms").IsRequired();

        builder.Property(r => r.CreatedBinderId).HasColumnName("created_binder_id");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => r.RunAt).HasDatabaseName("ix_scan_runs_run_at");
        builder.HasIndex(r => r.DestinationType).HasDatabaseName("ix_scan_runs_destination_type");

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.ScanRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ScanRun.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
