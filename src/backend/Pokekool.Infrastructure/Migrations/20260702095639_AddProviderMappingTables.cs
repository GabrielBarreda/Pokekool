using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderMappingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Créer les nouvelles tables ─────────────────────────────────
            migrationBuilder.CreateTable(
                name: "provider_card_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_card_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_card_mappings_pokemon_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "pokemon_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_set_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_set_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_set_mappings_pokemon_sets_set_id",
                        column: x => x.set_id,
                        principalTable: "pokemon_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_provider_card_mappings_provider_key",
                table: "provider_card_mappings",
                columns: new[] { "provider", "key" });

            migrationBuilder.CreateIndex(
                name: "ux_provider_card_mappings_card_provider_key",
                table: "provider_card_mappings",
                columns: new[] { "card_id", "provider", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_provider_set_mappings_provider_key",
                table: "provider_set_mappings",
                columns: new[] { "provider", "key" });

            migrationBuilder.CreateIndex(
                name: "ux_provider_set_mappings_set_provider_key",
                table: "provider_set_mappings",
                columns: new[] { "set_id", "provider", "key" },
                unique: true);

            // ── 2. Migrer les données existantes ──────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO provider_set_mappings (id, set_id, provider, key, value, created_at)
                SELECT gen_random_uuid(), id, 'pricecharting', 'slug', pricecharting_slug, NOW()
                FROM pokemon_sets WHERE pricecharting_slug IS NOT NULL;

                INSERT INTO provider_set_mappings (id, set_id, provider, key, value, created_at)
                SELECT gen_random_uuid(), id, 'cardmarket', 'expansion_id', cm_expansion_id::text, NOW()
                FROM pokemon_sets WHERE cm_expansion_id IS NOT NULL;

                INSERT INTO provider_set_mappings (id, set_id, provider, key, value, created_at)
                SELECT gen_random_uuid(), id, 'tcgplayer', 'group_id', tcgplayer_group_id::text, NOW()
                FROM pokemon_sets WHERE tcgplayer_group_id IS NOT NULL;

                INSERT INTO provider_card_mappings (id, card_id, provider, key, value, created_at)
                SELECT gen_random_uuid(), id, 'cardmarket', 'product_id', cm_product_id::text, NOW()
                FROM pokemon_cards WHERE cm_product_id IS NOT NULL;

                INSERT INTO provider_card_mappings (id, card_id, provider, key, value, created_at)
                SELECT gen_random_uuid(), id, 'cardmarket', 'scraping_slug', cm_scraping_slug, NOW()
                FROM pokemon_cards WHERE cm_scraping_slug IS NOT NULL;

                INSERT INTO provider_card_mappings (id, card_id, provider, key, value, created_at)
                SELECT gen_random_uuid(), id, 'tcgplayer', 'product_id', tcgplayer_product_id::text, NOW()
                FROM pokemon_cards WHERE tcgplayer_product_id IS NOT NULL;
                """);

            // ── 3. Supprimer les anciennes colonnes ───────────────────────────
            migrationBuilder.DropIndex(
                name: "ix_pokemon_cards_tcgplayer_product_id",
                table: "pokemon_cards");

            migrationBuilder.DropColumn(
                name: "cm_expansion_id",
                table: "pokemon_sets");

            migrationBuilder.DropColumn(
                name: "pricecharting_slug",
                table: "pokemon_sets");

            migrationBuilder.DropColumn(
                name: "tcgplayer_group_id",
                table: "pokemon_sets");

            migrationBuilder.DropColumn(
                name: "cm_product_id",
                table: "pokemon_cards");

            migrationBuilder.DropColumn(
                name: "cm_scraping_slug",
                table: "pokemon_cards");

            migrationBuilder.DropColumn(
                name: "tcgplayer_product_id",
                table: "pokemon_cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_card_mappings");

            migrationBuilder.DropTable(
                name: "provider_set_mappings");

            migrationBuilder.AddColumn<int>(
                name: "cm_expansion_id",
                table: "pokemon_sets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pricecharting_slug",
                table: "pokemon_sets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tcgplayer_group_id",
                table: "pokemon_sets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cm_product_id",
                table: "pokemon_cards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cm_scraping_slug",
                table: "pokemon_cards",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

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
    }
}
