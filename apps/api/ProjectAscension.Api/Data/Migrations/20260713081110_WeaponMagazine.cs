using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeaponMagazine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MagazineSize",
                table: "WeaponDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "ReloadTime",
                table: "WeaponDefinitions",
                type: "real",
                nullable: false,
                defaultValue: 0f);

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

            migrationBuilder.UpdateData(
                table: "WeaponDefinitions",
                keyColumn: "Key",
                keyValue: "pistol",
                columns: new[] { "MagazineSize", "ReloadTime" },
                values: new object[] { 12, 1.6f });

            migrationBuilder.UpdateData(
                table: "WeaponDefinitions",
                keyColumn: "Key",
                keyValue: "shield",
                columns: new[] { "MagazineSize", "ReloadTime" },
                values: new object[] { 0, 0f });

            migrationBuilder.UpdateData(
                table: "WeaponDefinitions",
                keyColumn: "Key",
                keyValue: "sword",
                columns: new[] { "MagazineSize", "ReloadTime" },
                values: new object[] { 0, 0f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MagazineSize",
                table: "WeaponDefinitions");

            migrationBuilder.DropColumn(
                name: "ReloadTime",
                table: "WeaponDefinitions");
        }
    }
}
