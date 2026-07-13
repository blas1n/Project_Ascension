using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ShieldAndBlockTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "BlockFrontArcDot",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "BlockReduction",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "CombatTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BlockFrontArcDot", "BlockReduction" },
                values: new object[] { 0.35f, 0.75f });

            migrationBuilder.InsertData(
                table: "WeaponDefinitions",
                columns: new[] { "Key", "ChargeTime", "Cooldown", "Damage", "DisplayName", "EquipmentType", "MaxChargeMultiplier", "ProjectileGravity", "ProjectileSpeed", "Range", "SlotType", "SpreadMax", "SpreadMin", "SpreadPerShot", "SpreadRecovery" },
                values: new object[] { "shield", 0f, 0f, 0f, "Kite Shield", 2, 1f, 0f, 0f, 0f, 0, 0f, 0f, 0f, 0f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "WeaponDefinitions",
                keyColumn: "Key",
                keyValue: "shield");

            migrationBuilder.DropColumn(
                name: "BlockFrontArcDot",
                table: "CombatTuningSettings");

            migrationBuilder.DropColumn(
                name: "BlockReduction",
                table: "CombatTuningSettings");
        }
    }
}
