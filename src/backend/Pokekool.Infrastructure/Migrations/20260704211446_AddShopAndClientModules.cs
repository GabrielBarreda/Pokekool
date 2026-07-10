using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopAndClientModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    zip_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    google_maps_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hours = table.Column<string>(type: "jsonb", nullable: true),
                    services = table.Column<string[]>(type: "jsonb", nullable: false),
                    instagram_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tiktok_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    photo_urls = table.Column<string[]>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shops", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shop_appointments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    client_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    service_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes_client = table.Column<string>(type: "text", nullable: true),
                    notes_internal = table.Column<string>(type: "text", nullable: true),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_appointments", x => x.id);
                    table.ForeignKey(
                        name: "FK_shop_appointments_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    condition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    grading_company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    grade_value = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_shop_products_pokemon_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "pokemon_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_shop_products_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    condition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    grading_company = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    grade_value = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    quantity_total = table.Column<int>(type: "integer", nullable: false),
                    quantity_available = table.Column<int>(type: "integer", nullable: false),
                    quantity_reserved = table.Column<int>(type: "integer", nullable: false),
                    purchase_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    purchase_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_stock", x => x.id);
                    table.ForeignKey(
                        name: "FK_shop_stock_pokemon_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "pokemon_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_shop_stock_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_request_shop_products_shop_product_id",
                        column: x => x.shop_product_id,
                        principalTable: "shop_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_request_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_product_id",
                table: "purchase_request",
                column: "shop_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_status",
                table: "purchase_request",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_user_id",
                table: "purchase_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shop_appointments_scheduled_at",
                table: "shop_appointments",
                column: "scheduled_at");

            migrationBuilder.CreateIndex(
                name: "ix_shop_appointments_shop_id",
                table: "shop_appointments",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "ix_shop_appointments_status",
                table: "shop_appointments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_shop_products_card_id",
                table: "shop_products",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "ix_shop_products_is_available",
                table: "shop_products",
                column: "is_available");

            migrationBuilder.CreateIndex(
                name: "ix_shop_products_shop_id",
                table: "shop_products",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "ix_shop_stock_card_id",
                table: "shop_stock",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "ix_shop_stock_shop_id",
                table: "shop_stock",
                column: "shop_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_request");

            migrationBuilder.DropTable(
                name: "shop_appointments");

            migrationBuilder.DropTable(
                name: "shop_stock");

            migrationBuilder.DropTable(
                name: "shop_products");

            migrationBuilder.DropTable(
                name: "shops");
        }
    }
}
