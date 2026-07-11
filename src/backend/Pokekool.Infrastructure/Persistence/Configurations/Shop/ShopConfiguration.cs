using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Infrastructure.Persistence.Configurations.Shop;

public sealed class ShopConfiguration : IEntityTypeConfiguration<Domain.Entities.Shop.Shop>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Shop.Shop> builder)
    {
        builder.ToTable("shops");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasColumnName("description");
        builder.Property(s => s.Address).HasColumnName("address").HasMaxLength(300);
        builder.Property(s => s.City).HasColumnName("city").HasMaxLength(120);
        builder.Property(s => s.ZipCode).HasColumnName("zip_code").HasMaxLength(20);
        builder.Property(s => s.GoogleMapsUrl).HasColumnName("google_maps_url").HasMaxLength(500);
        builder.Property(s => s.HoursJson).HasColumnName("hours").HasColumnType("jsonb");
        builder.Property(s => s.Services).HasColumnName("services").HasColumnType("jsonb");
        builder.Property(s => s.InstagramUrl).HasColumnName("instagram_url").HasMaxLength(500);
        builder.Property(s => s.TiktokUrl).HasColumnName("tiktok_url").HasMaxLength(500);
        builder.Property(s => s.PhotoUrls).HasColumnName("photo_urls").HasColumnType("jsonb");
        builder.Property(s => s.PlvModel3dUrl).HasColumnName("plv_model_3d_url").HasMaxLength(500);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
    }
}
