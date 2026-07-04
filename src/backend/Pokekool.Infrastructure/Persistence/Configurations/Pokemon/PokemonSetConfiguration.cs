using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class PokemonSetConfiguration : IEntityTypeConfiguration<PokemonSet>
{
    public void Configure(EntityTypeBuilder<PokemonSet> builder)
    {
        builder.ToTable("pokemon_sets");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.TcgId).HasColumnName("tcg_id").HasMaxLength(80).IsRequired();
        builder.Property(s => s.SeriesId).HasColumnName("series_id").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(s => s.Series).HasColumnName("series").HasMaxLength(255);
        builder.Property(s => s.ReleaseDate).HasColumnName("release_date");
        builder.Property(s => s.TotalCards).HasColumnName("total_cards");
        builder.Property(s => s.PrintedTotal).HasColumnName("printed_total");
        builder.Property(s => s.SymbolUrl).HasColumnName("symbol_url").HasMaxLength(500);
        builder.Property(s => s.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(s => s.TcgId)
            .IsUnique()
            .HasDatabaseName("ux_pokemon_sets_tcg_id");

        // Card relationship is configured in PokemonCardConfiguration
    }
}
