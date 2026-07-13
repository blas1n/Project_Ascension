using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeepGuardian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MonsterDefinitions",
                columns: new[] { "Key", "AggroRange", "AttackCooldown", "AttackRange", "AttackWindup", "Damage", "DropAmount", "DropItemKey", "MaxHealth", "MoveSpeed", "ProjectileSpeed", "Scale" },
                values: new object[] { "guardian", 45f, 1.4f, 20f, 0.9f, 45f, 3, "core", 600f, 3.2f, 26f, 2.6f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MonsterDefinitions",
                keyColumn: "Key",
                keyValue: "guardian");
        }
    }
}
