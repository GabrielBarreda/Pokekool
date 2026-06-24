using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Infrastructure.Persistence;

public sealed class PokekoolDbContext : DbContext
{
    public PokekoolDbContext(DbContextOptions<PokekoolDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<PokemonSeries> PokemonSeries => Set<PokemonSeries>();
    public DbSet<PokemonSet> PokemonSets => Set<PokemonSet>();
    public DbSet<PokemonCard> PokemonCards => Set<PokemonCard>();
    public DbSet<CardMarketPrice> CardMarketPrices => Set<CardMarketPrice>();
    public DbSet<CardPopulation> CardPopulations => Set<CardPopulation>();
    public DbSet<ProviderSetting> ProviderSettings => Set<ProviderSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PokekoolDbContext).Assembly);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Role>().HasQueryFilter(r => r.TenantId == null || true);
    }
}
