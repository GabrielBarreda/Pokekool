using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTcgPlayerIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── tcgplayer_group_id sur pokemon_sets ───────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "tcgplayer_group_id",
                table: "pokemon_sets",
                type: "integer",
                nullable: true);

            // ── tcgplayer_product_id sur pokemon_cards ────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "tcgplayer_product_id",
                table: "pokemon_cards",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_pokemon_cards_tcgplayer_product_id",
                table: "pokemon_cards",
                column: "tcgplayer_product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pokemon_cards_tcgplayer_product_id",
                table: "pokemon_cards");

            migrationBuilder.DropColumn(
                name: "tcgplayer_product_id",
                table: "pokemon_cards");

            migrationBuilder.DropColumn(
                name: "tcgplayer_group_id",
                table: "pokemon_sets");
        }
    }
}
