using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Infrastructure.Persistence.Configurations.Shop;

public sealed class ShopProductConfiguration : IEntityTypeConfiguration<ShopProduct>
{
    public void Configure(EntityTypeBuilder<ShopProduct> builder)
    {
        builder.ToTable("shop_products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.ShopId).HasColumnName("shop_id").IsRequired();
        builder.Property(p => p.Type).HasColumnName("type")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.CardId).HasColumnName("card_id");
        builder.Property(p => p.ProductName).HasColumnName("product_name").HasMaxLength(255);
        builder.Property(p => p.Language).HasColumnName("language").HasMaxLength(5).IsRequired().HasDefaultValue("EN");
        builder.Property(p => p.Condition).HasColumnName("condition").HasMaxLength(10);
        builder.Property(p => p.GradingCompany).HasColumnName("grading_company").HasMaxLength(20);
        builder.Property(p => p.GradeValue).HasColumnName("grade_value").HasPrecision(4, 1);
        builder.Property(p => p.Price).HasColumnName("price").HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(p => p.IsAvailable).HasColumnName("is_available").IsRequired();
        builder.Property(p => p.Description).HasColumnName("description");
        builder.Property(p => p.PhotoUrl).HasColumnName("photo_url").HasMaxLength(500);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => p.ShopId).HasDatabaseName("ix_shop_products_shop_id");
        builder.HasIndex(p => p.CardId).HasDatabaseName("ix_shop_products_card_id");
        builder.HasIndex(p => p.IsAvailable).HasDatabaseName("ix_shop_products_is_available");

        builder.HasOne<Domain.Entities.Shop.Shop>()
            .WithMany()
            .HasForeignKey(p => p.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Entities.Pokemon.PokemonCard>()
            .WithMany()
            .HasForeignKey(p => p.CardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
