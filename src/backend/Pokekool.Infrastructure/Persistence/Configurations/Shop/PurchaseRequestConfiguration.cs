using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Infrastructure.Persistence.Configurations.Shop;

public sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("purchase_request");

        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.Id).HasColumnName("id");
        builder.Property(pr => pr.ShopProductId).HasColumnName("shop_product_id").IsRequired();
        builder.Property(pr => pr.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(pr => pr.ClientName).HasColumnName("client_name").HasMaxLength(200).IsRequired();
        builder.Property(pr => pr.ClientEmail).HasColumnName("client_email").HasMaxLength(320).IsRequired();
        builder.Property(pr => pr.Message).HasColumnName("message").HasMaxLength(2000);

        builder.Property(pr => pr.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(pr => pr.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(pr => pr.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(pr => pr.UserId).HasDatabaseName("ix_purchase_request_user_id");
        builder.HasIndex(pr => pr.ShopProductId).HasDatabaseName("ix_purchase_request_product_id");
        builder.HasIndex(pr => pr.Status).HasDatabaseName("ix_purchase_request_status");

        // FK vers le produit du présentoir (entité gérée par le module Boutique).
        builder.HasOne(pr => pr.ShopProduct)
            .WithMany()
            .HasForeignKey(pr => pr.ShopProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK vers l'utilisateur (client authentifié) — obligatoire.
        builder.HasOne(pr => pr.User)
            .WithMany()
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
