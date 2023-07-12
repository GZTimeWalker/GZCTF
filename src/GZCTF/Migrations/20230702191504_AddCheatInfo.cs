using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddCheatInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheatInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    SubmitTeamId = table.Column<int>(type: "integer", nullable: false),
                    SourceTeamId = table.Column<int>(type: "integer", nullable: false),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheatInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheatInfo_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheatInfo_Participations_SourceTeamId",
                        column: x => x.SourceTeamId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheatInfo_Participations_SubmitTeamId",
                        column: x => x.SubmitTeamId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheatInfo_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Participations_TeamId",
                table: "Participations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatInfo_GameId",
                table: "CheatInfo",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatInfo_SourceTeamId",
                table: "CheatInfo",
                column: "SourceTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatInfo_SubmissionId",
                table: "CheatInfo",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatInfo_SubmitTeamId",
                table: "CheatInfo",
                column: "SubmitTeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheatInfo");

            migrationBuilder.DropIndex(
                name: "IX_Participations_TeamId",
                table: "Participations");
        }
    }
}
