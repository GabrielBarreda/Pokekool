using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderSettingsAndCardMarketMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Table provider_settings ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "provider_settings",
                columns: table => new
                {
                    provider        = table.Column<string>(type: "character varying(50)",  maxLength: 50,  nullable: false),
                    key             = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    encrypted_value = table.Column<string>(type: "text", nullable: false),
                    updated_at      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_settings", x => new { x.provider, x.key });
                });

            // ── cm_expansion_id sur pokemon_sets ──────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "cm_expansion_id",
                table: "pokemon_sets",
                type: "integer",
                nullable: true);

            // ── cm_product_id sur pokemon_cards ───────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "cm_product_id",
                table: "pokemon_cards",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_pokemon_cards_cm_product_id",
                table: "pokemon_cards",
                column: "cm_product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "provider_settings");

            migrationBuilder.DropIndex(
                name: "ix_pokemon_cards_cm_product_id",
                table: "pokemon_cards");

            migrationBuilder.DropColumn(name: "cm_expansion_id", table: "pokemon_sets");
            migrationBuilder.DropColumn(name: "cm_product_id",   table: "pokemon_cards");
        }
    }
}
