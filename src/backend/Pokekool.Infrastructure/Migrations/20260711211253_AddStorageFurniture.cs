using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageFurniture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "furniture_id",
                table: "storage_box",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "level_index",
                table: "storage_box",
                type: "smallint",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "box_id",
                table: "storage_binder",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "furniture_id",
                table: "storage_binder",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "level_index",
                table: "storage_binder",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "storage_furniture",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    position_3d = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_furniture", x => x.id);
                    table.ForeignKey(
                        name: "FK_storage_furniture_storage_room_room_id",
                        column: x => x.room_id,
                        principalTable: "storage_room",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_storage_box_furniture_id",
                table: "storage_box",
                column: "furniture_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_binder_furniture_id",
                table: "storage_binder",
                column: "furniture_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_furniture_room_id",
                table: "storage_furniture",
                column: "room_id");

            migrationBuilder.AddForeignKey(
                name: "FK_storage_binder_storage_furniture_furniture_id",
                table: "storage_binder",
                column: "furniture_id",
                principalTable: "storage_furniture",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_storage_box_storage_furniture_furniture_id",
                table: "storage_box",
                column: "furniture_id",
                principalTable: "storage_furniture",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_storage_binder_storage_furniture_furniture_id",
                table: "storage_binder");

            migrationBuilder.DropForeignKey(
                name: "FK_storage_box_storage_furniture_furniture_id",
                table: "storage_box");

            migrationBuilder.DropTable(
                name: "storage_furniture");

            migrationBuilder.DropIndex(
                name: "ix_storage_box_furniture_id",
                table: "storage_box");

            migrationBuilder.DropIndex(
                name: "ix_storage_binder_furniture_id",
                table: "storage_binder");

            migrationBuilder.DropColumn(
                name: "furniture_id",
                table: "storage_box");

            migrationBuilder.DropColumn(
                name: "level_index",
                table: "storage_box");

            migrationBuilder.DropColumn(
                name: "furniture_id",
                table: "storage_binder");

            migrationBuilder.DropColumn(
                name: "level_index",
                table: "storage_binder");

            migrationBuilder.AlterColumn<Guid>(
                name: "box_id",
                table: "storage_binder",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
