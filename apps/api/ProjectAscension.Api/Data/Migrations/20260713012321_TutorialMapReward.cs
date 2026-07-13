using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TutorialMapReward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000002-0000-0000-0000-000000000002"),
                column: "RewardJson",
                value: "{\"currency\":20,\"reputation\":4,\"itemKey\":\"frontier_map\",\"itemAmount\":1}");

            migrationBuilder.InsertData(
                table: "ItemDefinitions",
                columns: new[] { "Key", "BuyPrice", "Description", "DisplayName", "SellPrice" },
                values: new object[] { "frontier_map", 0, "Charted ground beyond the wall. Ink, hide, and someone's survival.", "Frontier Map", 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ItemDefinitions",
                keyColumn: "Key",
                keyValue: "frontier_map");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000002-0000-0000-0000-000000000002"),
                column: "RewardJson",
                value: "{\"currency\":80,\"reputation\":4}");
        }
    }
}
