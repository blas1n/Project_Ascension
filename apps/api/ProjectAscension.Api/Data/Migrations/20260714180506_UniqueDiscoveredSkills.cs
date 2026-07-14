using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueDiscoveredSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DiscoverySkills_EffectGraphJson_UniqueWhenReady",
                table: "DiscoverySkills",
                column: "EffectGraphJson",
                unique: true,
                filter: "\"Status\" = 'Ready'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscoverySkills_EffectGraphJson_UniqueWhenReady",
                table: "DiscoverySkills");
        }
    }
}
