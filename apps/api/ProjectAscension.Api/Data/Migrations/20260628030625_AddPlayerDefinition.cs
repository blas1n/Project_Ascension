using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MaxHealth = table.Column<float>(type: "real", nullable: false),
                    MoveSpeed = table.Column<float>(type: "real", nullable: false),
                    JumpVelocity = table.Column<float>(type: "real", nullable: false),
                    Gravity = table.Column<float>(type: "real", nullable: false),
                    DodgeSpeed = table.Column<float>(type: "real", nullable: false),
                    DodgeDuration = table.Column<float>(type: "real", nullable: false),
                    MaxFocus = table.Column<float>(type: "real", nullable: false),
                    FocusRegenPerSecond = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDefinitions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PlayerDefinitions",
                columns: new[] { "Id", "DodgeDuration", "DodgeSpeed", "FocusRegenPerSecond", "Gravity", "JumpVelocity", "MaxFocus", "MaxHealth", "MoveSpeed" },
                values: new object[] { 1, 0.2f, 12f, 15f, 20f, 6f, 100f, 100f, 5f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerDefinitions");
        }
    }
}
