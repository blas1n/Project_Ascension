using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BindingCombatLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "BindingCombatLockSeconds",
                table: "CombatTuningSettings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "CombatTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "BindingCombatLockSeconds",
                value: 3f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BindingCombatLockSeconds",
                table: "CombatTuningSettings");
        }
    }
}
