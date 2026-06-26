using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoveryTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BehaviorWeights",
                columns: table => new
                {
                    Behavior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BehaviorWeights", x => x.Behavior);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveryTuningSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DefaultBehaviorWeight = table.Column<int>(type: "integer", nullable: false),
                    PersistenceWeight = table.Column<int>(type: "integer", nullable: false),
                    CombinationSynergy = table.Column<int>(type: "integer", nullable: false),
                    FireThreshold = table.Column<int>(type: "integer", nullable: false),
                    BudgetBase = table.Column<int>(type: "integer", nullable: false),
                    BudgetPerScore = table.Column<double>(type: "double precision", nullable: false),
                    BudgetMin = table.Column<int>(type: "integer", nullable: false),
                    BudgetMax = table.Column<int>(type: "integer", nullable: false),
                    UncommonScore = table.Column<int>(type: "integer", nullable: false),
                    RareScore = table.Column<int>(type: "integer", nullable: false),
                    EpicScore = table.Column<int>(type: "integer", nullable: false),
                    LegendaryScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryTuningSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BehaviorWeights",
                columns: new[] { "Behavior", "Weight" },
                values: new object[,]
                {
                    { "ChargeAttack", 3 },
                    { "Dodge", 1 },
                    { "DodgeAttack", 3 },
                    { "Jump", 1 },
                    { "MeleeAttack", 2 },
                    { "RangedAttack", 2 }
                });

            migrationBuilder.InsertData(
                table: "DiscoveryTuningSettings",
                columns: new[] { "Id", "BudgetBase", "BudgetMax", "BudgetMin", "BudgetPerScore", "CombinationSynergy", "DefaultBehaviorWeight", "EpicScore", "FireThreshold", "LegendaryScore", "PersistenceWeight", "RareScore", "UncommonScore" },
                values: new object[] { 1, 8, 64, 16, 0.17999999999999999, 15, 1, 200, 100, 250, 5, 150, 120 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BehaviorWeights");

            migrationBuilder.DropTable(
                name: "DiscoveryTuningSettings");
        }
    }
}
