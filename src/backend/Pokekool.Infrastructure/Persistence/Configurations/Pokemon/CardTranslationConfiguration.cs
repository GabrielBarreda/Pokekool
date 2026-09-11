using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class CardTranslationConfiguration : IEntityTypeConfiguration<CardTranslation>
{
    public void Configure(EntityTypeBuilder<CardTranslation> builder)
    {
        builder.ToTable("card_translations");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.CardId).HasColumnName("card_id").IsRequired();
        builder.Property(t => t.Language).HasColumnName("language").HasMaxLength(8).IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");

        // Une seule traduction par carte et par langue — évite les doublons et sert de clé
        // de lookup pour le JOIN paramétré (WHERE card_id = ? AND language = @language).
        builder.HasIndex(t => new { t.CardId, t.Language })
            .IsUnique()
            .HasDatabaseName("ux_card_translations_card_id_language");

        builder.HasOne(t => t.Card)
            .WithMany(c => c.Translations)
            .HasForeignKey(t => t.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
