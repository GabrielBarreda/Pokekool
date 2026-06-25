using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pokemon_cards_cm_product_id",
                table: "pokemon_cards");

            migrationBuilder.CreateTable(
                name: "user_collection",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_owned = table.Column<bool>(type: "boolean", nullable: false),
                    is_favorite = table.Column<bool>(type: "boolean", nullable: false),
                    is_priority = table.Column<bool>(type: "boolean", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_collection", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_collection_pokemon_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "pokemon_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_collection_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_collection_card_id",
                table: "user_collection",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_collection_user_favorite",
                table: "user_collection",
                columns: new[] { "user_id", "is_favorite" });

            migrationBuilder.CreateIndex(
                name: "ix_user_collection_user_id",
                table: "user_collection",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_collection_user_owned",
                table: "user_collection",
                columns: new[] { "user_id", "is_owned" });

            migrationBuilder.CreateIndex(
                name: "ux_user_collection_user_card",
                table: "user_collection",
                columns: new[] { "user_id", "card_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_collection");

            migrationBuilder.CreateIndex(
                name: "ix_pokemon_cards_cm_product_id",
                table: "pokemon_cards",
                column: "cm_product_id");
        }
    }
}
