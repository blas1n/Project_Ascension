using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContextFactorWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultFactorWeight",
                table: "DiscoveryTuningSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FactorWeights",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactorWeights", x => x.Key);
                });

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DefaultFactorWeight",
                value: 0);

            migrationBuilder.InsertData(
                table: "FactorWeights",
                columns: new[] { "Key", "Category", "Weight" },
                values: new object[,]
                {
                    { "bow", "Equipment", 4 },
                    { "catalyst", "Equipment", 6 },
                    { "compression", "Knowledge", 8 },
                    { "crystal_desert", "Environment", 12 },
                    { "fire", "Knowledge", 8 },
                    { "ice_wall", "Environment", 10 },
                    { "jungle", "Environment", 8 },
                    { "pistol", "Equipment", 4 },
                    { "sword", "Equipment", 4 },
                    { "waterfall", "Environment", 10 },
                    { "wind", "Knowledge", 8 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactorWeights");

            migrationBuilder.DropColumn(
                name: "DefaultFactorWeight",
                table: "DiscoveryTuningSettings");
        }
    }
}
