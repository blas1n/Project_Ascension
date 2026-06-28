using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContractReputation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"),
                column: "RewardJson",
                value: "{\"currency\":120,\"reputation\":5}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000002-0000-0000-0000-000000000002"),
                column: "RewardJson",
                value: "{\"currency\":80,\"reputation\":4}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000003-0000-0000-0000-000000000003"),
                column: "RewardJson",
                value: "{\"currency\":90,\"reputation\":3}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000004-0000-0000-0000-000000000004"),
                column: "RewardJson",
                value: "{\"currency\":220,\"reputation\":8}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000005-0000-0000-0000-000000000005"),
                column: "RewardJson",
                value: "{\"currency\":400,\"reputation\":12}");

            migrationBuilder.InsertData(
                table: "Contracts",
                columns: new[] { "Id", "AssigneeActorId", "CompletedAt", "ConditionsJson", "CreatedAt", "DelegationAllowed", "Description", "ExpiresAt", "FailedAt", "IssuerActorId", "Kind", "ProgressCount", "Purpose", "RewardJson", "StartsAt", "Status", "Title" },
                values: new object[] { new Guid("c0000006-0000-0000-0000-000000000006"), null, null, "{\"targetCount\":3,\"target\":\"elite\",\"minReputation\":20}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "A trusted expeditionary's charge: slay 3 elites. (Requires standing.)", null, null, null, "Task", 0, "Hunt", "{\"currency\":350,\"reputation\":15}", null, "Open", "Frontier Warden" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000006-0000-0000-0000-000000000006"));

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"),
                column: "RewardJson",
                value: "{\"currency\":120}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000002-0000-0000-0000-000000000002"),
                column: "RewardJson",
                value: "{\"currency\":80}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000003-0000-0000-0000-000000000003"),
                column: "RewardJson",
                value: "{\"currency\":90}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000004-0000-0000-0000-000000000004"),
                column: "RewardJson",
                value: "{\"currency\":220}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000005-0000-0000-0000-000000000005"),
                column: "RewardJson",
                value: "{\"currency\":400}");
        }
    }
}
