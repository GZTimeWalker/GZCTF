using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTFServer.Migrations
{
    public partial class AddUserParticipation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Teams_ActiveTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Teams_OwnedTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ActiveTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OwnedTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActiveTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OwnedTeamId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastContainerOperation",
                table: "Instances",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "UserParticipations",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserParticipations", x => new { x.GameId, x.TeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserParticipations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserParticipations_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserParticipations_Participations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserParticipations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserParticipations_ParticipationId",
                table: "UserParticipations",
                column: "ParticipationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserParticipations_TeamId",
                table: "UserParticipations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_UserParticipations_UserId_GameId",
                table: "UserParticipations",
                columns: new[] { "UserId", "GameId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserParticipations");

            migrationBuilder.DropColumn(
                name: "LastContainerOperation",
                table: "Instances");

            migrationBuilder.AddColumn<int>(
                name: "ActiveTeamId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnedTeamId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ActiveTeamId",
                table: "AspNetUsers",
                column: "ActiveTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OwnedTeamId",
                table: "AspNetUsers",
                column: "OwnedTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Teams_ActiveTeamId",
                table: "AspNetUsers",
                column: "ActiveTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Teams_OwnedTeamId",
                table: "AspNetUsers",
                column: "OwnedTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
