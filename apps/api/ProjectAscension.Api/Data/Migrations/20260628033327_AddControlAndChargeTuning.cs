using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddControlAndChargeTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ChargedAttackThreshold",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "KnockbackPerMagnitude",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "SlowPerMagnitude",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "CombatTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ChargedAttackThreshold", "KnockbackPerMagnitude", "SlowPerMagnitude" },
                values: new object[] { 0.7f, 4f, 0.15f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargedAttackThreshold",
                table: "CombatTuningSettings");

            migrationBuilder.DropColumn(
                name: "KnockbackPerMagnitude",
                table: "CombatTuningSettings");

            migrationBuilder.DropColumn(
                name: "SlowPerMagnitude",
                table: "CombatTuningSettings");
        }
    }
}
