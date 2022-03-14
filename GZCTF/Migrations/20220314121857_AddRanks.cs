using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class AddRanks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ranks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChallengeId = table.Column<int>(type: "integer", nullable: true),
                    FirstId = table.Column<int>(type: "integer", nullable: true),
                    FirstTeamName = table.Column<string>(type: "text", nullable: true),
                    SecondId = table.Column<int>(type: "integer", nullable: true),
                    SecondTeamName = table.Column<string>(type: "text", nullable: true),
                    ThirdId = table.Column<int>(type: "integer", nullable: true),
                    ThirdTeamName = table.Column<string>(type: "text", nullable: true),
                    GameId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ranks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ranks_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ranks_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ranks_Teams_FirstId",
                        column: x => x.FirstId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ranks_Teams_SecondId",
                        column: x => x.SecondId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ranks_Teams_ThirdId",
                        column: x => x.ThirdId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ranks_ChallengeId",
                table: "Ranks",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Ranks_FirstId",
                table: "Ranks",
                column: "FirstId");

            migrationBuilder.CreateIndex(
                name: "IX_Ranks_GameId",
                table: "Ranks",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Ranks_SecondId",
                table: "Ranks",
                column: "SecondId");

            migrationBuilder.CreateIndex(
                name: "IX_Ranks_ThirdId",
                table: "Ranks",
                column: "ThirdId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ranks");
        }
    }
}
