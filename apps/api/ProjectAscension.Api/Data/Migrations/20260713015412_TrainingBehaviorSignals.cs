using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrainingBehaviorSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BehaviorWeights",
                columns: new[] { "Behavior", "Weight" },
                values: new object[,]
                {
                    { "AirAttack", 3 },
                    { "RepeatedJump", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "AirAttack");

            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "RepeatedJump");
        }
    }
}
