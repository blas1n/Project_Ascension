using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DelegationTutorialContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Contracts",
                columns: new[] { "Id", "AssigneeActorId", "CompletedAt", "ConditionsJson", "CreatedAt", "DelegationAllowed", "Description", "ExpiresAt", "FailedAt", "IssuerActorId", "Kind", "ProgressCount", "Purpose", "RewardJson", "StartsAt", "Status", "Title" },
                values: new object[] { new Guid("c0000005-0000-0000-0000-000000000005"), null, null, "{\"targetCount\":4,\"target\":\"elite\"}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Slay 4 elite monsters in the deep frontier. (Hard — consider delegating.)", null, null, null, "Task", 0, "Hunt", "{\"currency\":400}", null, "Open", "Deep Cull" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000005-0000-0000-0000-000000000005"));
        }
    }
}
