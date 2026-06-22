using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAscension.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContractProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgressCount",
                table: "Contracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgressCount",
                table: "Contracts");
        }
    }
}
