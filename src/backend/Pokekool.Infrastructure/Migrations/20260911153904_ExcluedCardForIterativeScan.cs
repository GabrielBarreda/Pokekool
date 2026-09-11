using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pokekool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExcluedCardForIterativeScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValueSql "'{}'" : tableau vide pour les lignes déjà existantes (tests
            // précédents) — sans ça, ADD COLUMN ... NOT NULL échoue sur ces lignes qui n'ont
            // aucune valeur à y mettre ("column contains null values").
            migrationBuilder.AddColumn<List<Guid>>(
                name: "excluded_card_ids",
                table: "scan_run_items",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "excluded_card_ids",
                table: "scan_run_items");
        }
    }
}
