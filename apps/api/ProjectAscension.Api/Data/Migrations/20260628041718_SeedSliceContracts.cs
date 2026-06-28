using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedSliceContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Contracts",
                columns: new[] { "Id", "AssigneeActorId", "CompletedAt", "ConditionsJson", "CreatedAt", "DelegationAllowed", "Description", "ExpiresAt", "FailedAt", "IssuerActorId", "Kind", "ProgressCount", "Purpose", "RewardJson", "StartsAt", "Status", "Title" },
                values: new object[,]
                {
                    { new Guid("c0000001-0000-0000-0000-000000000001"), null, null, "{\"targetCount\":5}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Defeat 5 monsters in the frontier.", null, null, null, "Task", 0, "Hunt", "{\"currency\":120}", null, "Open", "Cull the Beasts" },
                    { new Guid("c0000002-0000-0000-0000-000000000002"), null, null, "{\"targetCount\":1}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Reach the survey marker.", null, null, null, "Task", 0, "Survey", "{\"currency\":80}", null, "Open", "Map the Frontier" },
                    { new Guid("c0000003-0000-0000-0000-000000000003"), null, null, "{\"targetCount\":3}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Collect 3 samples.", null, null, null, "Task", 0, "Collection", "{\"currency\":90}", null, "Open", "Gather Samples" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000002-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000003-0000-0000-0000-000000000003"));
        }
    }
}
