using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Api.Data.Migrations
{
    [DbContext(typeof(LancamentosDbContext))]
    [Migration("20260727150000_AddExternalIdToEntries")]
    public partial class AddExternalIdToEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalId",
                table: "entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_entries_ExternalId",
                table: "entries",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_entries_ExternalId",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "entries");
        }
    }
}
