using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeaponDefinitions",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EquipmentType = table.Column<int>(type: "integer", nullable: false),
                    SlotType = table.Column<int>(type: "integer", nullable: false),
                    Damage = table.Column<float>(type: "real", nullable: false),
                    Range = table.Column<float>(type: "real", nullable: false),
                    ProjectileSpeed = table.Column<float>(type: "real", nullable: false),
                    ProjectileGravity = table.Column<float>(type: "real", nullable: false),
                    Cooldown = table.Column<float>(type: "real", nullable: false),
                    ChargeTime = table.Column<float>(type: "real", nullable: false),
                    MaxChargeMultiplier = table.Column<float>(type: "real", nullable: false),
                    SpreadMin = table.Column<float>(type: "real", nullable: false),
                    SpreadMax = table.Column<float>(type: "real", nullable: false),
                    SpreadPerShot = table.Column<float>(type: "real", nullable: false),
                    SpreadRecovery = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponDefinitions", x => x.Key);
                });

            migrationBuilder.InsertData(
                table: "WeaponDefinitions",
                columns: new[] { "Key", "ChargeTime", "Cooldown", "Damage", "DisplayName", "EquipmentType", "MaxChargeMultiplier", "ProjectileGravity", "ProjectileSpeed", "Range", "SlotType", "SpreadMax", "SpreadMin", "SpreadPerShot", "SpreadRecovery" },
                values: new object[,]
                {
                    { "bow", 0.8f, 0.6f, 18f, "Bow", 3, 2.5f, 9.8f, 28f, 60f, 3, 0f, 0f, 0f, 0f },
                    { "catalyst", 0f, 0.8f, 22f, "Arcane Catalyst", 5, 1f, 0f, 18f, 50f, 2, 0f, 0f, 0f, 0f },
                    { "pistol", 0f, 0.25f, 12f, "Pistol", 4, 1f, 0f, 0f, 60f, 2, 9f, 1f, 1.4f, 7f },
                    { "sword", 0f, 0.5f, 25f, "Sword", 0, 1f, 0f, 0f, 2.2f, 2, 0f, 0f, 0f, 0f }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeaponDefinitions");
        }
    }
}
