using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class PokemonCardConfiguration : IEntityTypeConfiguration<PokemonCard>
{
    public void Configure(EntityTypeBuilder<PokemonCard> builder)
    {
        builder.ToTable("pokemon_cards");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TcgId).HasColumnName("tcg_id").HasMaxLength(80).IsRequired();
        builder.Property(c => c.SetId).HasColumnName("set_id").IsRequired();
        builder.Property(c => c.Number).HasColumnName("number").HasMaxLength(20).IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(c => c.Supertype).HasColumnName("supertype").HasMaxLength(50);
        builder.Property(c => c.Subtypes).HasColumnName("subtypes").HasColumnType("jsonb");
        builder.Property(c => c.Rarity).HasColumnName("rarity").HasMaxLength(100);
        builder.Property(c => c.Types).HasColumnName("types").HasColumnType("jsonb");
        builder.Property(c => c.Hp).HasColumnName("hp");
        builder.Property(c => c.ImageSmall).HasColumnName("image_small").HasMaxLength(500);
        builder.Property(c => c.ImageLarge).HasColumnName("image_large").HasMaxLength(500);
        builder.Property(c => c.IsLegal).HasColumnName("is_legal").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        // Embedding visuel (DINOv2, 384 dim) — shadow property : le Domain ne connaît pas ce
        // champ (aucune propriété correspondante sur PokemonCard), afin de ne pas lui faire
        // porter de dépendance vers Pgvector.Vector (type spécifique PostgreSQL/EF). Accès via
        // IPokemonCardRepository.SetEmbeddingAsync / GetWithoutEmbeddingAsync / SearchByEmbeddingAsync
        // (voir PokemonCardRepository), qui exposent uniquement des float[] au reste de l'app.
        builder.Property<Vector>(PokemonCardEmbeddingColumn.ShadowPropertyName)
            .HasColumnName(PokemonCardEmbeddingColumn.ColumnName)
            .HasColumnType($"vector({PokemonCardEmbeddingColumn.Dimensions})");

        builder.HasIndex(c => c.TcgId)
            .IsUnique()
            .HasDatabaseName("ux_pokemon_cards_tcg_id");

        builder.HasIndex(c => c.SetId)
            .HasDatabaseName("ix_pokemon_cards_set_id");

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("ix_pokemon_cards_name");

        builder.HasIndex(c => c.Rarity)
            .HasDatabaseName("ix_pokemon_cards_rarity");

        builder.HasOne(c => c.Set)
            .WithMany(s => s.Cards)
            .HasForeignKey(c => c.SetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
