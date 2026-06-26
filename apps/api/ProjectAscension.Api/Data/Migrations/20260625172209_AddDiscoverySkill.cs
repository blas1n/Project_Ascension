using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoverySkill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscoverySkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscoveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Theme = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContextTagsJson = table.Column<string>(type: "text", nullable: false),
                    PrimaryBehavior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PowerBudget = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PrimitivesJson = table.Column<string>(type: "text", nullable: true),
                    PowerCost = table.Column<int>(type: "integer", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ComposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoverySkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscoverySkills_Discoveries_DiscoveryId",
                        column: x => x.DiscoveryId,
                        principalTable: "Discoveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoverySkills_DiscoveryId",
                table: "DiscoverySkills",
                column: "DiscoveryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoverySkills_Status",
                table: "DiscoverySkills",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoverySkills");
        }
    }
}
