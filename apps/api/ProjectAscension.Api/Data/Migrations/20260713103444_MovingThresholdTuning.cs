using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovingThresholdTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "MovingDistanceThreshold",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "CombatTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "MovingDistanceThreshold",
                value: 0.02f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MovingDistanceThreshold",
                table: "CombatTuningSettings");
        }
    }
}
