using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddToCollectionAfterScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_binder_id",
                table: "scan_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_price",
                table: "scan_run_items",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "estimated_price_currency",
                table: "scan_run_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_binder_id",
                table: "scan_runs");

            migrationBuilder.DropColumn(
                name: "estimated_price",
                table: "scan_run_items");

            migrationBuilder.DropColumn(
                name: "estimated_price_currency",
                table: "scan_run_items");
        }
    }
}
