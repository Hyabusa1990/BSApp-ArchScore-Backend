using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fawkes.Api.Migrations
{
    /// <inheritdoc />
    public partial class Update6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InitialMatchPointsLost",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InitialMatchPointsWon",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InitialRank",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InitialSetPointsLost",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InitialSetPointsWon",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InitialTotalScore",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rank",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RankDifference",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalScore",
                schema: "Core",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialMatchPointsLost",
                schema: "Core",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "InitialMatchPointsWon",
                schema: "Core",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "InitialRank",
                schema: "Core",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "InitialSetPointsLost",
                schema: "Core",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "InitialSetPointsWon",
                schema: "Core",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "InitialTotalScore",
                schema: "Core",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Rank",
                schema: "Core",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RankDifference",
                schema: "Core",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TotalScore",
                schema: "Core",
                table: "Teams");
        }
    }
}
