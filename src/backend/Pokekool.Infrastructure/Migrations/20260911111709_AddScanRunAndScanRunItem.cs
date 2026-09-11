using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScanRunAndScanRunItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scan_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    destination_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    added_to_stock_directly = table.Column<bool>(type: "boolean", nullable: true),
                    third_party_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    reason_other_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_photos = table.Column<int>(type: "integer", nullable: false),
                    succeeded = table.Column<int>(type: "integer", nullable: false),
                    failed = table.Column<int>(type: "integer", nullable: false),
                    average_distance = table.Column<double>(type: "double precision", nullable: true),
                    average_elapsed_ms = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scan_run_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scan_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    archived_photo_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    best_card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    best_card_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    best_set_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    best_set_release_year = table.Column<int>(type: "integer", nullable: true),
                    best_card_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    best_image_small = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    best_distance = table.Column<double>(type: "double precision", nullable: true),
                    elapsed_ms = table.Column<long>(type: "bigint", nullable: false),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_run_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_scan_run_items_scan_runs_scan_run_id",
                        column: x => x.scan_run_id,
                        principalTable: "scan_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scan_run_items_scan_run_id",
                table: "scan_run_items",
                column: "scan_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_scan_runs_destination_type",
                table: "scan_runs",
                column: "destination_type");

            migrationBuilder.CreateIndex(
                name: "ix_scan_runs_run_at",
                table: "scan_runs",
                column: "run_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scan_run_items");

            migrationBuilder.DropTable(
                name: "scan_runs");
        }
    }
}
