using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectileMagazines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "WeaponDefinitions",
                keyColumn: "Key",
                keyValue: "bow",
                columns: new[] { "MagazineSize", "ReloadTime" },
                values: new object[] { 8, 2.4f });

            migrationBuilder.UpdateData(
                table: "WeaponDefinitions",
                keyColumn: "Key",
                keyValue: "catalyst",
                columns: new[] { "MagazineSize", "ReloadTime" },
                values: new object[] { 5, 2.8f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "WeaponDefinitions",
                keyColumn: "Key",
                keyValue: "bow",
                columns: new[] { "MagazineSize", "ReloadTime" },
                values: new object[] { 0, 0f });

            migrationBuilder.UpdateData(
                table: "WeaponDefinitions",
                keyColumn: "Key",
                keyValue: "catalyst",
                columns: new[] { "MagazineSize", "ReloadTime" },
                values: new object[] { 0, 0f });
        }
    }
}
