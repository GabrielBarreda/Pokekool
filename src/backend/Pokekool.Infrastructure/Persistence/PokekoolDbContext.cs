using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Entities.Authorization;

namespace Pokekool.Infrastructure.Persistence;

public sealed class PokekoolDbContext : DbContext
{
    public PokekoolDbContext(DbContextOptions<PokekoolDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PokekoolDbContext).Assembly);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Role>().HasQueryFilter(r => r.TenantId == null || true);
    }
}
