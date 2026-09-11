using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "card_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_translations", x => x.id);
                    table.ForeignKey(
                        name: "FK_card_translations_pokemon_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "pokemon_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_card_translations_card_id_language",
                table: "card_translations",
                columns: new[] { "card_id", "language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_translations");
        }
    }
}
