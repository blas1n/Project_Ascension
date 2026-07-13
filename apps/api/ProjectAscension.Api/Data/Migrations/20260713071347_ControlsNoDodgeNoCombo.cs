using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ControlsNoDodgeNoCombo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "Dodge");

            migrationBuilder.DropColumn(
                name: "DodgeDuration",
                table: "PlayerDefinitions");

            migrationBuilder.DropColumn(
                name: "DodgeSpeed",
                table: "PlayerDefinitions");

            migrationBuilder.DropColumn(
                name: "InvocationComboJson",
                table: "DiscoverySkills");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "DodgeDuration",
                table: "PlayerDefinitions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "DodgeSpeed",
                table: "PlayerDefinitions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "InvocationComboJson",
                table: "DiscoverySkills",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "BehaviorWeights",
                columns: new[] { "Behavior", "Weight" },
                values: new object[] { "Dodge", 1 });

            migrationBuilder.UpdateData(
                table: "PlayerDefinitions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DodgeDuration", "DodgeSpeed" },
                values: new object[] { 0.2f, 12f });
        }
    }
}
