using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Entities.Shop;
using ShopEntity = Pokekool.Domain.Entities.Shop.Shop;

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
    public DbSet<CardGradedPrice> CardGradedPrices => Set<CardGradedPrice>();
    public DbSet<CardPopulation> CardPopulations => Set<CardPopulation>();
    public DbSet<ProviderSetMapping>  ProviderSetMappings  => Set<ProviderSetMapping>();
    public DbSet<ProviderCardMapping> ProviderCardMappings => Set<ProviderCardMapping>();
    public DbSet<ProviderSetting> ProviderSettings => Set<ProviderSetting>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<UserCollection> UserCollections => Set<UserCollection>();

    // ── Module Client ──────────────────────────────────────────
    // NB: les DbSet Shop/ShopProduct/ShopStock/ShopAppointment sont déclarés par le module Boutique.
    public DbSet<Pokekool.Domain.Entities.Shop.PurchaseRequest> PurchaseRequests => Set<Pokekool.Domain.Entities.Shop.PurchaseRequest>();

    // ---- Module Boutique ----
    public DbSet<ShopEntity> Shops => Set<ShopEntity>();
    public DbSet<ShopProduct> ShopProducts => Set<ShopProduct>();
    public DbSet<ShopStock> ShopStock => Set<ShopStock>();
    public DbSet<ShopAppointment> ShopAppointments => Set<ShopAppointment>();

    // ---- Module Stockage Physique ----
    public DbSet<Pokekool.Domain.Entities.Storage.StorageRoom> StorageRooms
        => Set<Pokekool.Domain.Entities.Storage.StorageRoom>();
    public DbSet<Pokekool.Domain.Entities.Storage.StorageBox> StorageBoxes
        => Set<Pokekool.Domain.Entities.Storage.StorageBox>();
    public DbSet<Pokekool.Domain.Entities.Storage.StorageBinder> StorageBinders
        => Set<Pokekool.Domain.Entities.Storage.StorageBinder>();
    public DbSet<Pokekool.Domain.Entities.Storage.StorageLocation> StorageLocations
        => Set<Pokekool.Domain.Entities.Storage.StorageLocation>();
    public DbSet<Pokekool.Domain.Entities.Storage.StorageMovementEvent> StorageMovementEvents
        => Set<Pokekool.Domain.Entities.Storage.StorageMovementEvent>();
    public DbSet<Pokekool.Domain.Entities.Storage.StorageModelCompileJob> StorageModelCompileJobs
        => Set<Pokekool.Domain.Entities.Storage.StorageModelCompileJob>();
    public DbSet<Pokekool.Domain.Entities.Storage.StorageFurniture> StorageFurnitures
        => Set<Pokekool.Domain.Entities.Storage.StorageFurniture>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PokekoolDbContext).Assembly);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Role>().HasQueryFilter(r => r.TenantId == null || true);
    }
}
