using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pokekool.Domain.Entities.Storage;

namespace Pokekool.Infrastructure.Persistence.Configurations.Storage;

public sealed class StorageRoomConfiguration : IEntityTypeConfiguration<StorageRoom>
{
    public void Configure(EntityTypeBuilder<StorageRoom> builder)
    {
        builder.ToTable("storage_room");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.ShopId).HasColumnName("shop_id").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.FloorPlanUrl).HasColumnName("floor_plan_url").HasMaxLength(500);
        builder.Property(r => r.Model3dUrl).HasColumnName("model_3d_url").HasMaxLength(500);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => r.ShopId).HasDatabaseName("ix_storage_room_shop_id");

        builder.HasOne<Domain.Entities.Shop.Shop>()
            .WithMany()
            .HasForeignKey(r => r.ShopId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StorageBoxConfiguration : IEntityTypeConfiguration<StorageBox>
{
    public void Configure(EntityTypeBuilder<StorageBox> builder)
    {
        builder.ToTable("storage_box");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.RoomId).HasColumnName("room_id").IsRequired();
        builder.Property(b => b.LabelOrigin).HasColumnName("label_origin")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.Barcode).HasColumnName("barcode").HasMaxLength(120);
        builder.Property(b => b.InternalCode).HasColumnName("internal_code").HasMaxLength(120);
        builder.Property(b => b.SupplierName).HasColumnName("supplier_name").HasMaxLength(200);
        builder.Property(b => b.Size).HasColumnName("size").HasMaxLength(4);
        builder.Property(b => b.Position3dJson).HasColumnName("position_3d").HasColumnType("jsonb");
        builder.Property(b => b.Notes).HasColumnName("notes");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(b => b.RoomId).HasDatabaseName("ix_storage_box_room_id");
        // Unicité conditionnelle : uniquement quand la colonne n'est pas nulle (index filtré PostgreSQL).
        builder.HasIndex(b => b.Barcode)
            .HasDatabaseName("ux_storage_box_barcode")
            .IsUnique()
            .HasFilter("barcode IS NOT NULL");
        builder.HasIndex(b => b.InternalCode)
            .HasDatabaseName("ux_storage_box_internal_code")
            .IsUnique()
            .HasFilter("internal_code IS NOT NULL");

        builder.HasOne<StorageRoom>()
            .WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StorageBinderConfiguration : IEntityTypeConfiguration<StorageBinder>
{
    public void Configure(EntityTypeBuilder<StorageBinder> builder)
    {
        builder.ToTable("storage_binder");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.BoxId).HasColumnName("box_id").IsRequired();
        builder.Property(b => b.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
        builder.Property(b => b.PositionInBox).HasColumnName("position_in_box");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(b => b.BoxId).HasDatabaseName("ix_storage_binder_box_id");

        builder.HasOne<StorageBox>()
            .WithMany()
            .HasForeignKey(b => b.BoxId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.ToTable("storage_location");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.BinderId).HasColumnName("binder_id").IsRequired();
        builder.Property(l => l.OwnerType).HasColumnName("owner_type").HasMaxLength(30).IsRequired();
        builder.Property(l => l.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(l => l.SlotPosition).HasColumnName("slot_position");
        builder.Property(l => l.MovedAt).HasColumnName("moved_at").IsRequired();
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(l => l.BinderId).HasDatabaseName("ix_storage_location_binder_id");
        // Référence polymorphe : index (owner_type, owner_id), aucune FK DB stricte.
        builder.HasIndex(l => new { l.OwnerType, l.OwnerId })
            .HasDatabaseName("ix_storage_location_owner");

        builder.HasOne<StorageBinder>()
            .WithMany()
            .HasForeignKey(l => l.BinderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StorageMovementEventConfiguration : IEntityTypeConfiguration<StorageMovementEvent>
{
    public void Configure(EntityTypeBuilder<StorageMovementEvent> builder)
    {
        builder.ToTable("storage_movement_event");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.BoxId).HasColumnName("box_id");
        builder.Property(m => m.DetectedAt).HasColumnName("detected_at").IsRequired();
        builder.Property(m => m.FromPosition3dJson).HasColumnName("from_position_3d").HasColumnType("jsonb");
        builder.Property(m => m.ToPosition3dJson).HasColumnName("to_position_3d").HasColumnType("jsonb");
        builder.Property(m => m.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.ConfirmedBy).HasColumnName("confirmed_by");
        builder.Property(m => m.ConfirmedAt).HasColumnName("confirmed_at");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(m => m.Status).HasDatabaseName("ix_storage_movement_event_status");

        builder.HasOne<StorageBox>()
            .WithMany()
            .HasForeignKey(m => m.BoxId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class StorageModelCompileJobConfiguration : IEntityTypeConfiguration<StorageModelCompileJob>
{
    public void Configure(EntityTypeBuilder<StorageModelCompileJob> builder)
    {
        builder.ToTable("storage_model_compile_job");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id");
        builder.Property(j => j.TargetType).HasColumnName("target_type").HasMaxLength(30).IsRequired();
        builder.Property(j => j.TargetId).HasColumnName("target_id").IsRequired();
        builder.Property(j => j.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(j => j.BlendFilePath).HasColumnName("blend_file_path").HasMaxLength(1000).IsRequired();
        builder.Property(j => j.GlbOutputPath).HasColumnName("glb_output_path").HasMaxLength(1000);
        builder.Property(j => j.ErrorMessage).HasColumnName("error_message");
        builder.Property(j => j.RequestedBy).HasColumnName("requested_by").IsRequired();
        builder.Property(j => j.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at");

        builder.HasIndex(j => new { j.TargetType, j.TargetId })
            .HasDatabaseName("ix_storage_model_compile_job_target");
    }
}
