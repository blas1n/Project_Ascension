using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedMonsterFactorWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "FactorWeights",
                columns: new[] { "Key", "Category", "Weight" },
                values: new object[,]
                {
                    { "monster:elite", "Monster", 14 },
                    { "monster:melee", "Monster", 6 },
                    { "monster:ranged", "Monster", 8 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "monster:elite");

            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "monster:melee");

            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "monster:ranged");
        }
    }
}
