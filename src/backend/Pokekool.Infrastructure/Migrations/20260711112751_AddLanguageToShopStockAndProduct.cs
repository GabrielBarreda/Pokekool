using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageToShopStockAndProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "shop_stock",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "EN");

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "shop_products",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "EN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "language",
                table: "shop_stock");

            migrationBuilder.DropColumn(
                name: "language",
                table: "shop_products");
        }
    }
}
