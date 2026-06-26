using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoveryGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KnowledgeDepthWeight",
                table: "DiscoveryTuningSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DiscoveryLineages",
                columns: table => new
                {
                    ChildDiscoveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentDiscoveryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryLineages", x => new { x.ChildDiscoveryId, x.ParentDiscoveryId });
                    table.ForeignKey(
                        name: "FK_DiscoveryLineages_Discoveries_ChildDiscoveryId",
                        column: x => x.ChildDiscoveryId,
                        principalTable: "Discoveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscoveryLineages_Discoveries_ParentDiscoveryId",
                        column: x => x.ParentDiscoveryId,
                        principalTable: "Discoveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "KnowledgeDepthWeight",
                value: 12);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryLineages_ChildDiscoveryId",
                table: "DiscoveryLineages",
                column: "ChildDiscoveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryLineages_ParentDiscoveryId",
                table: "DiscoveryLineages",
                column: "ParentDiscoveryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoveryLineages");

            migrationBuilder.DropColumn(
                name: "KnowledgeDepthWeight",
                table: "DiscoveryTuningSettings");
        }
    }
}
