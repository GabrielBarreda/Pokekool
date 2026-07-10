using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Infrastructure.Persistence.Configurations.Shop;

public sealed class ShopStockConfiguration : IEntityTypeConfiguration<ShopStock>
{
    public void Configure(EntityTypeBuilder<ShopStock> builder)
    {
        builder.ToTable("shop_stock");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.ShopId).HasColumnName("shop_id").IsRequired();
        builder.Property(s => s.CardId).HasColumnName("card_id");
        builder.Property(s => s.ProductName).HasColumnName("product_name").HasMaxLength(255);
        builder.Property(s => s.Type).HasColumnName("type")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(s => s.Condition).HasColumnName("condition").HasMaxLength(10);
        builder.Property(s => s.GradingCompany).HasColumnName("grading_company").HasMaxLength(20);
        builder.Property(s => s.GradeValue).HasColumnName("grade_value").HasPrecision(4, 1);
        builder.Property(s => s.QuantityTotal).HasColumnName("quantity_total").IsRequired();
        builder.Property(s => s.QuantityAvailable).HasColumnName("quantity_available").IsRequired();
        builder.Property(s => s.QuantityReserved).HasColumnName("quantity_reserved").IsRequired();
        builder.Property(s => s.PurchasePrice).HasColumnName("purchase_price").HasPrecision(12, 2);
        builder.Property(s => s.PurchaseCurrency).HasColumnName("purchase_currency").HasMaxLength(3);
        builder.Property(s => s.Notes).HasColumnName("notes");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(s => s.ShopId).HasDatabaseName("ix_shop_stock_shop_id");
        builder.HasIndex(s => s.CardId).HasDatabaseName("ix_shop_stock_card_id");

        builder.HasOne<Domain.Entities.Shop.Shop>()
            .WithMany()
            .HasForeignKey(s => s.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Entities.Pokemon.PokemonCard>()
            .WithMany()
            .HasForeignKey(s => s.CardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
