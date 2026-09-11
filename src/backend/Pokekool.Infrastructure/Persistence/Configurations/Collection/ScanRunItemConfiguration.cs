using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Collection;

namespace Pokekool.Infrastructure.Persistence.Configurations.Collection;

public sealed class ScanRunItemConfiguration : IEntityTypeConfiguration<ScanRunItem>
{
    public void Configure(EntityTypeBuilder<ScanRunItem> builder)
    {
        builder.ToTable("scan_run_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.ScanRunId).HasColumnName("scan_run_id").IsRequired();

        builder.Property(i => i.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
        builder.Property(i => i.ArchivedPhotoFileName).HasColumnName("archived_photo_file_name").HasMaxLength(500);
        builder.Property(i => i.Success).HasColumnName("success").IsRequired();

        builder.Property(i => i.BestCardId).HasColumnName("best_card_id");
        builder.Property(i => i.BestCardName).HasColumnName("best_card_name").HasMaxLength(300);
        builder.Property(i => i.BestSetName).HasColumnName("best_set_name").HasMaxLength(300);
        builder.Property(i => i.BestSetReleaseYear).HasColumnName("best_set_release_year");
        builder.Property(i => i.BestCardNumber).HasColumnName("best_card_number").HasMaxLength(30);
        builder.Property(i => i.BestImageSmall).HasColumnName("best_image_small").HasMaxLength(1000);
        builder.Property(i => i.BestDistance).HasColumnName("best_distance");

        builder.Property(i => i.ElapsedMs).HasColumnName("elapsed_ms").IsRequired();
        builder.Property(i => i.Error).HasColumnName("error").HasMaxLength(1000);

        builder.Property(i => i.EstimatedPrice).HasColumnName("estimated_price").HasPrecision(10, 2);
        builder.Property(i => i.EstimatedPriceCurrency).HasColumnName("estimated_price_currency").HasMaxLength(3);

        // Historique cumulé des refus utilisateur pour cette photo (voir ScanRunItem.AddExcludedCardId) —
        // simple tableau Postgres (Npgsql supporte Guid[] nativement), pas une vraie relation FK.
        // Mappé directement sur le champ privé _excludedCardIds (List<Guid>, mutable) et NON sur
        // la propriété publique ExcludedCardIds (IReadOnlyCollection<Guid>, lecture seule) :
        // `builder.Property(i => i.ExcludedCardIds)` fait échouer `dotnet ef migrations add` avec
        // "Read-only collections of primitive types are not supported" — EF Core 8+ refuse de
        // mapper une collection de primitifs en lecture seule. L'accès par nom de champ ci-dessous
        // contourne le problème en ciblant directement le List<Guid> mutable qui la sous-tend.
        builder.Ignore(i => i.ExcludedCardIds);
        builder.Property<List<Guid>>("_excludedCardIds")
            .HasColumnName("excluded_card_ids")
            .HasColumnType("uuid[]")
            .IsRequired();

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(i => i.ScanRunId).HasDatabaseName("ix_scan_run_items_scan_run_id");
    }
}
