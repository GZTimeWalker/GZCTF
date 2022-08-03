using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class UpdateParticipation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_ParticipationId_ChallengeId_GameId",
                table: "Submissions");

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ParticipationId",
                table: "Submissions",
                column: "ParticipationId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TeamId_ChallengeId_GameId",
                table: "Submissions",
                columns: new[] { "TeamId", "ChallengeId", "GameId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Teams_TeamId",
                table: "Submissions",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Teams_TeamId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_ParticipationId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_TeamId_ChallengeId_GameId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Submissions");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ParticipationId_ChallengeId_GameId",
                table: "Submissions",
                columns: new[] { "ParticipationId", "ChallengeId", "GameId" });
        }
    }
}
