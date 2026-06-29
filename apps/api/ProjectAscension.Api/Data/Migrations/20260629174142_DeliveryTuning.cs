using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "DeliveryAreaRadius",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DeliveryHitscanRadius",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DeliveryProjectileGravity",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DeliveryProjectileSpeed",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DeliveryRange",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "CombatTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryAreaRadius", "DeliveryHitscanRadius", "DeliveryProjectileGravity", "DeliveryProjectileSpeed", "DeliveryRange" },
                values: new object[] { 4f, 1.5f, 0f, 32f, 60f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAreaRadius",
                table: "CombatTuningSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryHitscanRadius",
                table: "CombatTuningSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryProjectileGravity",
                table: "CombatTuningSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryProjectileSpeed",
                table: "CombatTuningSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryRange",
                table: "CombatTuningSettings");
        }
    }
}
