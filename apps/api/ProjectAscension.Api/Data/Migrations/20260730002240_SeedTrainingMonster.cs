using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedTrainingMonster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MonsterDefinitions",
                columns: new[] { "Key", "AggroRange", "AttackCooldown", "AttackRange", "AttackWindup", "Damage", "DropAmount", "DropItemKey", "MaxHealth", "MoveSpeed", "ProjectileSpeed", "Scale" },
                values: new object[] { "training", 9f, 1.6f, 2f, 1f, 3f, 0, "", 30f, 2f, 0f, 1f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MonsterDefinitions",
                keyColumn: "Key",
                keyValue: "training");
        }
    }
}
