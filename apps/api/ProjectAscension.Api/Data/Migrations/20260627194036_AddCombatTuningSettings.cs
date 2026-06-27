using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatTuningSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CombatTuningSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ProjectileDamage = table.Column<float>(type: "real", nullable: false),
                    BeamDamage = table.Column<float>(type: "real", nullable: false),
                    AreaDamage = table.Column<float>(type: "real", nullable: false),
                    DotDamagePerTick = table.Column<float>(type: "real", nullable: false),
                    SpreadFalloff = table.Column<float>(type: "real", nullable: false),
                    BaseDotTicks = table.Column<int>(type: "integer", nullable: false),
                    ShieldPerMagnitude = table.Column<float>(type: "real", nullable: false),
                    DashPerMagnitude = table.Column<float>(type: "real", nullable: false),
                    LeechFractionPerMagnitude = table.Column<float>(type: "real", nullable: false),
                    ControlDurationPerMagnitude = table.Column<float>(type: "real", nullable: false),
                    PassiveShieldReduction = table.Column<float>(type: "real", nullable: false),
                    PassiveBarrierReduction = table.Column<float>(type: "real", nullable: false),
                    PassiveLeech = table.Column<float>(type: "real", nullable: false),
                    FocusCostPerPoint = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatTuningSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CombatTuningSettings",
                columns: new[] { "Id", "AreaDamage", "BaseDotTicks", "BeamDamage", "ControlDurationPerMagnitude", "DashPerMagnitude", "DotDamagePerTick", "FocusCostPerPoint", "LeechFractionPerMagnitude", "PassiveBarrierReduction", "PassiveLeech", "PassiveShieldReduction", "ProjectileDamage", "ShieldPerMagnitude", "SpreadFalloff" },
                values: new object[] { 1, 8f, 2, 9f, 0.6f, 2f, 3f, 4f, 0.15f, 0.08f, 0.05f, 0.06f, 10f, 12f, 0.6f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CombatTuningSettings");
        }
    }
}
