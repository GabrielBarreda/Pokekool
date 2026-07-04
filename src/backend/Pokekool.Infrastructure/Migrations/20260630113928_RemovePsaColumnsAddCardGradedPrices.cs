using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePsaColumnsAddCardGradedPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "psa1",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa10",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa2",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa3",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa4",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa5",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa6",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa7",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa8",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa9",
                table: "card_market_prices");

            migrationBuilder.DropColumn(
                name: "psa9_5",
                table: "card_market_prices");

            migrationBuilder.CreateTable(
                name: "card_graded_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    grading_company = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    grade_value = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_graded_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_card_graded_prices_pokemon_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "pokemon_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_card_graded_prices_card_id",
                table: "card_graded_prices",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "ux_card_graded_prices_card_source_company_grade",
                table: "card_graded_prices",
                columns: new[] { "card_id", "source", "grading_company", "grade_value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_graded_prices");

            migrationBuilder.AddColumn<decimal>(
                name: "psa1",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa10",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa2",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa3",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa4",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa5",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa6",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa7",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa8",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa9",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "psa9_5",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }
    }
}
