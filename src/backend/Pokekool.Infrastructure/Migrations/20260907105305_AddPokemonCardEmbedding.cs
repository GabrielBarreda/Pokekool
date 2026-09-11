using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPokemonCardEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "embedding",
                table: "pokemon_cards",
                type: "vector(384)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "embedding",
                table: "pokemon_cards");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
