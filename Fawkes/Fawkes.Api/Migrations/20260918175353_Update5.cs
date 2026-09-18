using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fawkes.Api.Migrations
{
    /// <inheritdoc />
    public partial class Update5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfirmedSet01Score",
                schema: "Core",
                table: "TargetAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedSet02Score",
                schema: "Core",
                table: "TargetAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedSet03Score",
                schema: "Core",
                table: "TargetAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedSet04Score",
                schema: "Core",
                table: "TargetAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedSet05Score",
                schema: "Core",
                table: "TargetAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentSetNo",
                schema: "Core",
                table: "TargetAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Shots",
                schema: "Core",
                table: "TargetAssignments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CurrentRoundNo",
                schema: "Core",
                table: "Fixtures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RuleSetKey",
                schema: "Core",
                table: "Fixtures",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedSet01Score",
                schema: "Core",
                table: "TargetAssignments");

            migrationBuilder.DropColumn(
                name: "ConfirmedSet02Score",
                schema: "Core",
                table: "TargetAssignments");

            migrationBuilder.DropColumn(
                name: "ConfirmedSet03Score",
                schema: "Core",
                table: "TargetAssignments");

            migrationBuilder.DropColumn(
                name: "ConfirmedSet04Score",
                schema: "Core",
                table: "TargetAssignments");

            migrationBuilder.DropColumn(
                name: "ConfirmedSet05Score",
                schema: "Core",
                table: "TargetAssignments");

            migrationBuilder.DropColumn(
                name: "CurrentSetNo",
                schema: "Core",
                table: "TargetAssignments");

            migrationBuilder.DropColumn(
                name: "Shots",
                schema: "Core",
                table: "TargetAssignments");

            migrationBuilder.DropColumn(
                name: "CurrentRoundNo",
                schema: "Core",
                table: "Fixtures");

            migrationBuilder.DropColumn(
                name: "RuleSetKey",
                schema: "Core",
                table: "Fixtures");
        }
    }
}
