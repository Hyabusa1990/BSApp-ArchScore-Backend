using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fawkes.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedDisplayTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayTheme",
                schema: "Core",
                table: "Devices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayTheme",
                schema: "Core",
                table: "Devices");
        }
    }
}
