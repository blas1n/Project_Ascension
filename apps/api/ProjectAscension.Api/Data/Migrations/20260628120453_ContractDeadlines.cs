using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContractDeadlines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000004-0000-0000-0000-000000000004"),
                column: "ConditionsJson",
                value: "{\"targetCount\":2,\"target\":\"elite\",\"timeLimitSeconds\":120}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000006-0000-0000-0000-000000000006"),
                column: "ConditionsJson",
                value: "{\"targetCount\":3,\"target\":\"elite\",\"minReputation\":20,\"timeLimitSeconds\":180}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000004-0000-0000-0000-000000000004"),
                column: "ConditionsJson",
                value: "{\"targetCount\":2,\"target\":\"elite\"}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000006-0000-0000-0000-000000000006"),
                column: "ConditionsJson",
                value: "{\"targetCount\":3,\"target\":\"elite\",\"minReputation\":20}");
        }
    }
}
