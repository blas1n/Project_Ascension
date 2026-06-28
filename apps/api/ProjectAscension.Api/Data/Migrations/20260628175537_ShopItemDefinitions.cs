using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ShopItemDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemDefinitions",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SellPrice = table.Column<int>(type: "integer", nullable: false),
                    BuyPrice = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemDefinitions", x => x.Key);
                });

            migrationBuilder.InsertData(
                table: "ItemDefinitions",
                columns: new[] { "Key", "BuyPrice", "DisplayName", "SellPrice" },
                values: new object[,]
                {
                    { "core", 90, "Elite Core", 40 },
                    { "feather", 20, "Sky Feather", 10 },
                    { "hide", 16, "Beast Hide", 8 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemDefinitions");
        }
    }
}
