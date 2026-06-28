using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContractIssuing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractRewardTuning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    BaseRewardPerCount = table.Column<float>(type: "real", nullable: false),
                    DifficultyScale = table.Column<float>(type: "real", nullable: false),
                    BandMinPercent = table.Column<int>(type: "integer", nullable: false),
                    BandMaxPercent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractRewardTuning", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ContractRewardTuning",
                columns: new[] { "Id", "BandMaxPercent", "BandMinPercent", "BaseRewardPerCount", "DifficultyScale" },
                values: new object[] { 1, 150, 70, 25f, 0.4f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractRewardTuning");
        }
    }
}
