using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EffectGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EffectGraphJson",
                table: "DiscoverySkills",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectGraphJson",
                table: "DiscoverySkills");
        }
    }
}
