using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterAttackWindup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "AttackWindup",
                table: "MonsterDefinitions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.UpdateData(
                table: "MonsterDefinitions",
                keyColumn: "Key",
                keyValue: "elite",
                column: "AttackWindup",
                value: 0.65f);

            migrationBuilder.UpdateData(
                table: "MonsterDefinitions",
                keyColumn: "Key",
                keyValue: "melee",
                column: "AttackWindup",
                value: 0.35f);

            migrationBuilder.UpdateData(
                table: "MonsterDefinitions",
                keyColumn: "Key",
                keyValue: "ranged",
                column: "AttackWindup",
                value: 0.5f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttackWindup",
                table: "MonsterDefinitions");
        }
    }
}
