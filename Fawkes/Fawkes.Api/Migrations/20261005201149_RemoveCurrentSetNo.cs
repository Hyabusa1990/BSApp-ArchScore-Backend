using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fawkes.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCurrentSetNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSetNo",
                schema: "Core",
                table: "TargetAssignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSetNo",
                schema: "Core",
                table: "TargetAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
