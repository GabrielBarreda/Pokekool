using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class CardGradedPriceConfiguration : IEntityTypeConfiguration<CardGradedPrice>
{
    public void Configure(EntityTypeBuilder<CardGradedPrice> builder)
    {
        builder.ToTable("card_graded_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.CardId).HasColumnName("card_id").IsRequired();
        builder.Property(p => p.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
        builder.Property(p => p.GradingCompany).HasColumnName("grading_company").HasMaxLength(10).IsRequired();
        builder.Property(p => p.GradeValue).HasColumnName("grade_value").HasPrecision(3, 1).IsRequired();
        builder.Property(p => p.Price).HasColumnName("price").HasPrecision(14, 2).IsRequired();
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.FetchedAt).HasColumnName("fetched_at").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        // Clé naturelle : une seule entrée par (carte, source, société, grade)
        builder.HasIndex(p => new { p.CardId, p.Source, p.GradingCompany, p.GradeValue })
            .IsUnique()
            .HasDatabaseName("ux_card_graded_prices_card_source_company_grade");

        builder.HasIndex(p => p.CardId)
            .HasDatabaseName("ix_card_graded_prices_card_id");

        builder.HasOne(p => p.Card)
            .WithMany()
            .HasForeignKey(p => p.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
