using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoverySkillInvocationCombo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvocationComboJson",
                table: "DiscoverySkills",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvocationComboJson",
                table: "DiscoverySkills");
        }
    }
}
