using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class CardPopulationConfiguration : IEntityTypeConfiguration<CardPopulation>
{
    public void Configure(EntityTypeBuilder<CardPopulation> builder)
    {
        builder.ToTable("card_populations");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.CardId).HasColumnName("card_id").IsRequired();
        builder.Property(p => p.Source).HasColumnName("source").HasMaxLength(20).IsRequired();
        builder.Property(p => p.TotalGraded).HasColumnName("total_graded");
        builder.Property(p => p.Pop10).HasColumnName("pop10");
        builder.Property(p => p.Pop9).HasColumnName("pop9");
        builder.Property(p => p.Pop8).HasColumnName("pop8");
        builder.Property(p => p.Pop7).HasColumnName("pop7");
        builder.Property(p => p.GemRate10).HasColumnName("gem_rate10");
        builder.Property(p => p.FetchedAt).HasColumnName("fetched_at").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        // Index unique (card_id, source) — base de l'upsert
        builder.HasIndex(p => new { p.CardId, p.Source })
            .IsUnique()
            .HasDatabaseName("ux_card_populations_card_source");

        builder.HasIndex(p => p.CardId)
            .HasDatabaseName("ix_card_populations_card_id");

        builder.HasOne(p => p.Card)
            .WithMany()
            .HasForeignKey(p => p.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
