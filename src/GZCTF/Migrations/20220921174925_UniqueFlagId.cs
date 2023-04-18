using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class UniqueFlagId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserParticipations_UserId_GameId",
                table: "UserParticipations");

            migrationBuilder.DropIndex(
                name: "IX_Instances_FlagId",
                table: "Instances");

            migrationBuilder.DropIndex(
                name: "IX_Instances_ParticipationId_ChallengeId",
                table: "Instances");

            migrationBuilder.CreateIndex(
                name: "IX_UserParticipations_UserId_GameId",
                table: "UserParticipations",
                columns: new[] { "UserId", "GameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instances_FlagId",
                table: "Instances",
                column: "FlagId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instances_ParticipationId",
                table: "Instances",
                column: "ParticipationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserParticipations_UserId_GameId",
                table: "UserParticipations");

            migrationBuilder.DropIndex(
                name: "IX_Instances_FlagId",
                table: "Instances");

            migrationBuilder.DropIndex(
                name: "IX_Instances_ParticipationId",
                table: "Instances");

            migrationBuilder.CreateIndex(
                name: "IX_UserParticipations_UserId_GameId",
                table: "UserParticipations",
                columns: new[] { "UserId", "GameId" });

            migrationBuilder.CreateIndex(
                name: "IX_Instances_FlagId",
                table: "Instances",
                column: "FlagId");

            migrationBuilder.CreateIndex(
                name: "IX_Instances_ParticipationId_ChallengeId",
                table: "Instances",
                columns: new[] { "ParticipationId", "ChallengeId" });
        }
    }
}
