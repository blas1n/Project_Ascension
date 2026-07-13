using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DiscoveryEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BudgetPerScore",
                table: "DiscoveryTuningSettings",
                newName: "DepthCostGrowth");

            migrationBuilder.AddColumn<double>(
                name: "BudgetGrowth",
                table: "DiscoveryTuningSettings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BudgetBase", "BudgetGrowth", "BudgetMax", "BudgetMin", "DepthCostGrowth" },
                values: new object[] { 6, 2.3999999999999999, 40, 10, 1.3500000000000001 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetGrowth",
                table: "DiscoveryTuningSettings");

            migrationBuilder.RenameColumn(
                name: "DepthCostGrowth",
                table: "DiscoveryTuningSettings",
                newName: "BudgetPerScore");

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BudgetBase", "BudgetMax", "BudgetMin", "BudgetPerScore" },
                values: new object[] { 8, 64, 16, 0.17999999999999999 });
        }
    }
}
