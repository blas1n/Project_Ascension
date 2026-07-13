using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EconomyTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Licensed",
                table: "Knowledge",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LicensedAt",
                table: "Knowledge",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EconomyTuning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    KnowledgeGoldPerPoint = table.Column<int>(type: "integer", nullable: false),
                    KnowledgePointsPerRep = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomyTuning", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EconomyTuning",
                columns: new[] { "Id", "KnowledgeGoldPerPoint", "KnowledgePointsPerRep" },
                values: new object[] { 1, 6, 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EconomyTuning");

            migrationBuilder.DropColumn(
                name: "Licensed",
                table: "Knowledge");

            migrationBuilder.DropColumn(
                name: "LicensedAt",
                table: "Knowledge");
        }
    }
}
