using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoragePhysique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "storage_model_compile_job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    blend_file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    glb_output_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_model_compile_job", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "storage_room",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    floor_plan_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    model_3d_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_room", x => x.id);
                    table.ForeignKey(
                        name: "FK_storage_room_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "storage_box",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label_origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    barcode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    internal_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    size = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    position_3d = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_box", x => x.id);
                    table.ForeignKey(
                        name: "FK_storage_box_storage_room_room_id",
                        column: x => x.room_id,
                        principalTable: "storage_room",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "storage_binder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    box_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    position_in_box = table.Column<short>(type: "smallint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_binder", x => x.id);
                    table.ForeignKey(
                        name: "FK_storage_binder_storage_box_box_id",
                        column: x => x.box_id,
                        principalTable: "storage_box",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "storage_movement_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    box_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    from_position_3d = table.Column<string>(type: "jsonb", nullable: true),
                    to_position_3d = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_movement_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_storage_movement_event_storage_box_box_id",
                        column: x => x.box_id,
                        principalTable: "storage_box",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "storage_location",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    binder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_position = table.Column<short>(type: "smallint", nullable: true),
                    moved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_location", x => x.id);
                    table.ForeignKey(
                        name: "FK_storage_location_storage_binder_binder_id",
                        column: x => x.binder_id,
                        principalTable: "storage_binder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_storage_binder_box_id",
                table: "storage_binder",
                column: "box_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_box_room_id",
                table: "storage_box",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ux_storage_box_barcode",
                table: "storage_box",
                column: "barcode",
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_storage_box_internal_code",
                table: "storage_box",
                column: "internal_code",
                unique: true,
                filter: "internal_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_storage_location_binder_id",
                table: "storage_location",
                column: "binder_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_location_owner",
                table: "storage_location",
                columns: new[] { "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_storage_model_compile_job_target",
                table: "storage_model_compile_job",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_storage_movement_event_box_id",
                table: "storage_movement_event",
                column: "box_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_movement_event_status",
                table: "storage_movement_event",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_storage_room_shop_id",
                table: "storage_room",
                column: "shop_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "storage_location");

            migrationBuilder.DropTable(
                name: "storage_model_compile_job");

            migrationBuilder.DropTable(
                name: "storage_movement_event");

            migrationBuilder.DropTable(
                name: "storage_binder");

            migrationBuilder.DropTable(
                name: "storage_box");

            migrationBuilder.DropTable(
                name: "storage_room");
        }
    }
}
