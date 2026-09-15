using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fawkes.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedTeamsAndTargetAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FixtureId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MatchPointsWon = table.Column<int>(type: "integer", nullable: false),
                    MatchPointsLost = table.Column<int>(type: "integer", nullable: false),
                    SetPointsWon = table.Column<int>(type: "integer", nullable: false),
                    SetPointsLost = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Fixtures_FixtureId",
                        column: x => x.FixtureId,
                        principalSchema: "Core",
                        principalTable: "Fixtures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TargetAssignments",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FixtureId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    RoundNo = table.Column<int>(type: "integer", nullable: false),
                    TargetNo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetAssignments_Fixtures_FixtureId",
                        column: x => x.FixtureId,
                        principalSchema: "Core",
                        principalTable: "Fixtures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TargetAssignments_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Core",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TargetAssignments_FixtureId",
                schema: "Core",
                table: "TargetAssignments",
                column: "FixtureId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetAssignments_TeamId",
                schema: "Core",
                table: "TargetAssignments",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_FixtureId",
                schema: "Core",
                table: "Teams",
                column: "FixtureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TargetAssignments",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "Teams",
                schema: "Core");
        }
    }
}
