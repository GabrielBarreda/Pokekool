using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGradedPriceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<decimal>(
                name: "psa10",
                table: "card_market_prices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "psa7",   table: "card_market_prices");
            migrationBuilder.DropColumn(name: "psa8",   table: "card_market_prices");
            migrationBuilder.DropColumn(name: "psa9",   table: "card_market_prices");
            migrationBuilder.DropColumn(name: "psa9_5", table: "card_market_prices");
            migrationBuilder.DropColumn(name: "psa10",  table: "card_market_prices");
        }
    }
}
