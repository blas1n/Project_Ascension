using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompositionGrammar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "AirAttack");

            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "ChargeAttack");

            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "ChargedAttack");

            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "DodgeAttack");

            migrationBuilder.DeleteData(
                table: "BehaviorWeights",
                keyColumn: "Behavior",
                keyValue: "RepeatedJump");

            migrationBuilder.RenameColumn(
                name: "SynthesisWeight",
                table: "DiscoveryTuningSettings",
                newName: "SequenceWeight");

            migrationBuilder.AddColumn<int>(
                name: "ChainWeight",
                table: "DiscoveryTuningSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConcurrencyWeight",
                table: "DiscoveryTuningSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FuseWeight",
                table: "DiscoveryTuningSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ChainWeight", "ConcurrencyWeight", "FuseWeight", "SequenceWeight" },
                values: new object[] { 6, 12, 25, 15 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChainWeight",
                table: "DiscoveryTuningSettings");

            migrationBuilder.DropColumn(
                name: "ConcurrencyWeight",
                table: "DiscoveryTuningSettings");

            migrationBuilder.DropColumn(
                name: "FuseWeight",
                table: "DiscoveryTuningSettings");

            migrationBuilder.RenameColumn(
                name: "SequenceWeight",
                table: "DiscoveryTuningSettings",
                newName: "SynthesisWeight");

            migrationBuilder.InsertData(
                table: "BehaviorWeights",
                columns: new[] { "Behavior", "Weight" },
                values: new object[,]
                {
                    { "AirAttack", 3 },
                    { "ChargeAttack", 3 },
                    { "ChargedAttack", 3 },
                    { "DodgeAttack", 3 },
                    { "RepeatedJump", 2 }
                });

            migrationBuilder.UpdateData(
                table: "DiscoveryTuningSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "SynthesisWeight",
                value: 25);
        }
    }
}
