using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MonsterDrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DropAmount",
                table: "MonsterDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DropItemKey",
                table: "MonsterDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "MonsterDefinitions",
                keyColumn: "Key",
                keyValue: "elite",
                columns: new[] { "DropAmount", "DropItemKey" },
                values: new object[] { 1, "core" });

            migrationBuilder.UpdateData(
                table: "MonsterDefinitions",
                keyColumn: "Key",
                keyValue: "melee",
                columns: new[] { "DropAmount", "DropItemKey" },
                values: new object[] { 2, "hide" });

            migrationBuilder.UpdateData(
                table: "MonsterDefinitions",
                keyColumn: "Key",
                keyValue: "ranged",
                columns: new[] { "DropAmount", "DropItemKey" },
                values: new object[] { 2, "feather" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropAmount",
                table: "MonsterDefinitions");

            migrationBuilder.DropColumn(
                name: "DropItemKey",
                table: "MonsterDefinitions");
        }
    }
}
