using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPsa1To6Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "psa1",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "psa1",
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
        }
    }
}
