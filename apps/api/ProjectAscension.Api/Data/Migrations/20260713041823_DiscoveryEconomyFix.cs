using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DiscoveryEconomyFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepthCostGrowth",
                table: "DiscoveryTuningSettings");

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EpicScore", "LegendaryScore", "RareScore", "UncommonScore" },
                values: new object[] { 338, 506, 225, 150 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DepthCostGrowth",
                table: "DiscoveryTuningSettings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DepthCostGrowth", "EpicScore", "LegendaryScore", "RareScore", "UncommonScore" },
                values: new object[] { 1.3500000000000001, 200, 250, 150, 120 });
        }
    }
}
