using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SkillCooldowns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FocusRegenPerSecond",
                table: "PlayerDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxFocus",
                table: "PlayerDefinitions");

            migrationBuilder.RenameColumn(
                name: "FocusCostPerPoint",
                table: "CombatTuningSettings",
                newName: "CooldownSecondsPerPoint");

            migrationBuilder.AddColumn<float>(
                name: "CooldownCeilingSeconds",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "CooldownFloorSeconds",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "CombatTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CooldownCeilingSeconds", "CooldownFloorSeconds", "CooldownSecondsPerPoint" },
                values: new object[] { 12f, 2f, 0.3f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CooldownCeilingSeconds",
                table: "CombatTuningSettings");

            migrationBuilder.DropColumn(
                name: "CooldownFloorSeconds",
                table: "CombatTuningSettings");

            migrationBuilder.RenameColumn(
                name: "CooldownSecondsPerPoint",
                table: "CombatTuningSettings",
                newName: "FocusCostPerPoint");

            migrationBuilder.AddColumn<float>(
                name: "FocusRegenPerSecond",
                table: "PlayerDefinitions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "MaxFocus",
                table: "PlayerDefinitions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "CombatTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "FocusCostPerPoint",
                value: 4f);

            migrationBuilder.UpdateData(
                table: "PlayerDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FocusRegenPerSecond", "MaxFocus" },
                values: new object[] { 15f, 100f });
        }
    }
}
