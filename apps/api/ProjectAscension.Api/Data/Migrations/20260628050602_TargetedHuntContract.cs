using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TargetedHuntContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Contracts",
                columns: new[] { "Id", "AssigneeActorId", "CompletedAt", "ConditionsJson", "CreatedAt", "DelegationAllowed", "Description", "ExpiresAt", "FailedAt", "IssuerActorId", "Kind", "ProgressCount", "Purpose", "RewardJson", "StartsAt", "Status", "Title" },
                values: new object[] { new Guid("c0000004-0000-0000-0000-000000000004"), null, null, "{\"targetCount\":2,\"target\":\"elite\"}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Slay 2 elite monsters.", null, null, null, "Task", 0, "Hunt", "{\"currency\":220}", null, "Open", "Elite Bounty" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000004-0000-0000-0000-000000000004"));
        }
    }
}
