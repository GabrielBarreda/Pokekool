using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardPopulationAndPriceChartingSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Colonne pricecharting_slug sur pokemon_sets ───────────────────
            migrationBuilder.AddColumn<string>(
                name: "pricecharting_slug",
                table: "pokemon_sets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // ── Table card_populations ────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "card_populations",
                columns: table => new
                {
                    id           = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id      = table.Column<Guid>(type: "uuid", nullable: false),
                    source       = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_graded = table.Column<int>(type: "integer", nullable: true),
                    pop10        = table.Column<int>(type: "integer", nullable: true),
                    pop9         = table.Column<int>(type: "integer", nullable: true),
                    pop8         = table.Column<int>(type: "integer", nullable: true),
                    pop7         = table.Column<int>(type: "integer", nullable: true),
                    gem_rate10   = table.Column<double>(type: "double precision", nullable: true),
                    fetched_at   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_populations", x => x.id);
                    table.ForeignKey(
                        name: "FK_card_populations_pokemon_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "pokemon_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_card_populations_card_id",
                table: "card_populations",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "ux_card_populations_card_source",
                table: "card_populations",
                columns: new[] { "card_id", "source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_populations");

            migrationBuilder.DropColumn(
                name: "pricecharting_slug",
                table: "pokemon_sets");
        }
    }
}
