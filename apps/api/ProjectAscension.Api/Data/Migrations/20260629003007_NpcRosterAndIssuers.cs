using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NpcRosterAndIssuers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "NPCs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "NPCs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"),
                column: "ConditionsJson",
                value: "{\"targetCount\":5,\"issuer\":\"City Watch\"}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000002-0000-0000-0000-000000000002"),
                column: "ConditionsJson",
                value: "{\"targetCount\":1,\"issuer\":\"Survey Office\"}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000003-0000-0000-0000-000000000003"),
                column: "ConditionsJson",
                value: "{\"targetCount\":3,\"issuer\":\"Herbalist Guild\"}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000004-0000-0000-0000-000000000004"),
                column: "ConditionsJson",
                value: "{\"targetCount\":2,\"target\":\"elite\",\"timeLimitSeconds\":120,\"failOn\":[\"timeout\"],\"issuer\":\"Bounty Office\"}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000005-0000-0000-0000-000000000005"),
                column: "ConditionsJson",
                value: "{\"targetCount\":4,\"target\":\"elite\",\"issuer\":\"Frontier Command\"}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000006-0000-0000-0000-000000000006"),
                column: "ConditionsJson",
                value: "{\"targetCount\":3,\"target\":\"elite\",\"minReputation\":20,\"timeLimitSeconds\":180,\"failOn\":[\"timeout\",\"death\"],\"issuer\":\"Warden's Office\"}");

            migrationBuilder.InsertData(
                table: "NPCs",
                columns: new[] { "Id", "Alive", "CreatedAt", "CurrentRegionId", "HomeRegionId", "Name", "Role" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000001"), true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("33333333-3333-3333-3333-333333333333"), "Quartermaster Hale", "Shopkeeper" },
                    { new Guid("a0000002-0000-0000-0000-000000000002"), true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("33333333-3333-3333-3333-333333333333"), "Serjeant Bran", "Guard" },
                    { new Guid("a0000003-0000-0000-0000-000000000003"), true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("33333333-3333-3333-3333-333333333333"), "Survey Clerk Mira", "Contract Clerk" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NPCs",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "NPCs",
                keyColumn: "Id",
                keyValue: new Guid("a0000002-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "NPCs",
                keyColumn: "Id",
                keyValue: new Guid("a0000003-0000-0000-0000-000000000003"));

            migrationBuilder.DropColumn(
                name: "Role",
                table: "NPCs");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "NPCs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"),
                column: "ConditionsJson",
                value: "{\"targetCount\":5}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000002-0000-0000-0000-000000000002"),
                column: "ConditionsJson",
                value: "{\"targetCount\":1}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000003-0000-0000-0000-000000000003"),
                column: "ConditionsJson",
                value: "{\"targetCount\":3}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000004-0000-0000-0000-000000000004"),
                column: "ConditionsJson",
                value: "{\"targetCount\":2,\"target\":\"elite\",\"timeLimitSeconds\":120,\"failOn\":[\"timeout\"]}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000005-0000-0000-0000-000000000005"),
                column: "ConditionsJson",
                value: "{\"targetCount\":4,\"target\":\"elite\"}");

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: new Guid("c0000006-0000-0000-0000-000000000006"),
                column: "ConditionsJson",
                value: "{\"targetCount\":3,\"target\":\"elite\",\"minReputation\":20,\"timeLimitSeconds\":180,\"failOn\":[\"timeout\",\"death\"]}");
        }
    }
}
