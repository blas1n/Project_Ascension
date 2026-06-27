using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedChargedAttackWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BehaviorWeights",
                columns: new[] { "Behavior", "Weight" },
                values: new object[] { "ChargedAttack", 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "ChargedAttack");
        }
    }
}
