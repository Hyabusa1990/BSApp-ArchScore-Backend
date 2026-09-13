using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fawkes.Api.Migrations
{
    /// <inheritdoc />
    public partial class Update3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FixturePermission_Fixtures_FixtureId",
                schema: "Core",
                table: "FixturePermission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FixturePermission",
                schema: "Core",
                table: "FixturePermission");

            migrationBuilder.RenameTable(
                name: "FixturePermission",
                schema: "Core",
                newName: "FixturePermissions",
                newSchema: "Core");

            migrationBuilder.RenameIndex(
                name: "IX_FixturePermission_FixtureId",
                schema: "Core",
                table: "FixturePermissions",
                newName: "IX_FixturePermissions_FixtureId");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "Core",
                table: "Fixtures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                schema: "Core",
                table: "FixturePermissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FixturePermissions",
                schema: "Core",
                table: "FixturePermissions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FixturePermissions_Fixtures_FixtureId",
                schema: "Core",
                table: "FixturePermissions",
                column: "FixtureId",
                principalSchema: "Core",
                principalTable: "Fixtures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FixturePermissions_Fixtures_FixtureId",
                schema: "Core",
                table: "FixturePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FixturePermissions",
                schema: "Core",
                table: "FixturePermissions");

            migrationBuilder.DropColumn(
                name: "Location",
                schema: "Core",
                table: "Fixtures");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                schema: "Core",
                table: "FixturePermissions");

            migrationBuilder.RenameTable(
                name: "FixturePermissions",
                schema: "Core",
                newName: "FixturePermission",
                newSchema: "Core");

            migrationBuilder.RenameIndex(
                name: "IX_FixturePermissions_FixtureId",
                schema: "Core",
                table: "FixturePermission",
                newName: "IX_FixturePermission_FixtureId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FixturePermission",
                schema: "Core",
                table: "FixturePermission",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FixturePermission_Fixtures_FixtureId",
                schema: "Core",
                table: "FixturePermission",
                column: "FixtureId",
                principalSchema: "Core",
                principalTable: "Fixtures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
