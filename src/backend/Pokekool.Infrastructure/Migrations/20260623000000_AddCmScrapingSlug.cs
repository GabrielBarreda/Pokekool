using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCmScrapingSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Slug CardMarket optionnel sur pokemon_cards.
            // Null = slug généré automatiquement depuis le nom de la carte.
            // Override manuel pour les cartes dont le slug auto serait incorrect.
            migrationBuilder.AddColumn<string>(
                name: "cm_scraping_slug",
                table: "pokemon_cards",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cm_scraping_slug",
                table: "pokemon_cards");
        }
    }
}
