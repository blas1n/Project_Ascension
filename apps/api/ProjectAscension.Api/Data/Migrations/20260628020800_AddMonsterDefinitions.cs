using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonsterDefinitions",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MaxHealth = table.Column<float>(type: "real", nullable: false),
                    MoveSpeed = table.Column<float>(type: "real", nullable: false),
                    AggroRange = table.Column<float>(type: "real", nullable: false),
                    AttackRange = table.Column<float>(type: "real", nullable: false),
                    AttackCooldown = table.Column<float>(type: "real", nullable: false),
                    Damage = table.Column<float>(type: "real", nullable: false),
                    ProjectileSpeed = table.Column<float>(type: "real", nullable: false),
                    Scale = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonsterDefinitions", x => x.Key);
                });

            migrationBuilder.InsertData(
                table: "MonsterDefinitions",
                columns: new[] { "Key", "AggroRange", "AttackCooldown", "AttackRange", "Damage", "MaxHealth", "MoveSpeed", "ProjectileSpeed", "Scale" },
                values: new object[,]
                {
                    { "elite", 35f, 1.2f, 18f, 14f, 120f, 2.5f, 24f, 1.6f },
                    { "melee", 25f, 1f, 2f, 8f, 40f, 3.5f, 0f, 1f },
                    { "ranged", 30f, 1.5f, 14f, 6f, 25f, 2f, 18f, 1f }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonsterDefinitions");
        }
    }
}
