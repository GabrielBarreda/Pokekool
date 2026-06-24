using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

public sealed class PokemonSeriesConfiguration : IEntityTypeConfiguration<PokemonSeries>
{
    public void Configure(EntityTypeBuilder<PokemonSeries> builder)
    {
        builder.ToTable("pokemon_series");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.TcgId).HasColumnName("tcg_id").HasMaxLength(80).IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(s => s.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(s => s.TcgId)
            .IsUnique()
            .HasDatabaseName("ux_pokemon_series_tcg_id");

        builder.HasMany(s => s.Sets)
            .WithOne(set => set.SeriesEntity)
            .HasForeignKey(set => set.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
