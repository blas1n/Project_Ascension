using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ItemDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ItemDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "ItemDefinitions",
                keyColumn: "Key",
                keyValue: "core",
                column: "Description",
                value: "A volatile core torn from an elite — fortifies the outpost's defenses.");

            migrationBuilder.UpdateData(
                table: "ItemDefinitions",
                keyColumn: "Key",
                keyValue: "feather",
                column: "Description",
                value: "Light feathers prized in the city markets.");

            migrationBuilder.UpdateData(
                table: "ItemDefinitions",
                keyColumn: "Key",
                keyValue: "hide",
                column: "Description",
                value: "Tough hide from frontier beasts — raises shelters and armor.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ItemDefinitions");
        }
    }
}
