using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class CardMarketPriceConfiguration : IEntityTypeConfiguration<CardMarketPrice>
{
    public void Configure(EntityTypeBuilder<CardMarketPrice> builder)
    {
        builder.ToTable("card_market_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.CardId).HasColumnName("card_id").IsRequired();
        builder.Property(p => p.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
        builder.Property(p => p.Variant).HasColumnName("variant").HasMaxLength(50);
        builder.Property(p => p.MarketPrice).HasColumnName("market_price").HasPrecision(10, 2);
        builder.Property(p => p.LowPrice).HasColumnName("low_price").HasPrecision(10, 2);
        builder.Property(p => p.HighPrice).HasColumnName("high_price").HasPrecision(10, 2);
        builder.Property(p => p.Psa7).HasColumnName("psa7").HasPrecision(10, 2);
        builder.Property(p => p.Psa8).HasColumnName("psa8").HasPrecision(10, 2);
        builder.Property(p => p.Psa9).HasColumnName("psa9").HasPrecision(10, 2);
        builder.Property(p => p.Psa95).HasColumnName("psa9_5").HasPrecision(10, 2);
        builder.Property(p => p.Psa10).HasColumnName("psa10").HasPrecision(10, 2);
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.FetchedAt).HasColumnName("fetched_at").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        // Unique key : (card_id, source, variant) — base de l'upsert
        builder.HasIndex(p => new { p.CardId, p.Source, p.Variant })
            .IsUnique()
            .HasDatabaseName("ux_card_market_prices_card_source_variant");

        // Lookup rapide par carte
        builder.HasIndex(p => p.CardId)
            .HasDatabaseName("ix_card_market_prices_card_id");

        builder.HasOne(p => p.Card)
            .WithMany()
            .HasForeignKey(p => p.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
