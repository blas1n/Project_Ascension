using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DiscoveryScarcity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "catalyst");

            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "pistol");

            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "sword");

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CombinationSynergy", "EpicScore", "FireThreshold", "LegendaryScore", "RareScore", "UncommonScore" },
                values: new object[] { 10, 675, 200, 1013, 450, 300 });

            migrationBuilder.InsertData(
                table: "FactorWeights",
                columns: new[] { "Key", "Category", "Weight" },
                values: new object[,]
                {
                    { "arcane", "Equipment", 6 },
                    { "firearm", "Equipment", 4 },
                    { "melee", "Equipment", 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "arcane");

            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "firearm");

            migrationBuilder.DeleteData(
                table: "FactorWeights",
                keyColumn: "Key",
                keyValue: "melee");

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CombinationSynergy", "EpicScore", "FireThreshold", "LegendaryScore", "RareScore", "UncommonScore" },
                values: new object[] { 15, 338, 100, 506, 225, 150 });

            migrationBuilder.InsertData(
                table: "FactorWeights",
                columns: new[] { "Key", "Category", "Weight" },
                values: new object[,]
                {
                    { "catalyst", "Equipment", 6 },
                    { "pistol", "Equipment", 4 },
                    { "sword", "Equipment", 4 }
                });
        }
    }
}
