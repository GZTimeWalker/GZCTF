using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstSolves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSolved",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "AcceptedCount",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "IsSolved",
                table: "ExerciseInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "ExerciseInstances");

            migrationBuilder.DropColumn(
                name: "AcceptedCount",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "ExerciseChallenges");

            migrationBuilder.CreateTable(
                name: "FirstSolves",
                columns: table => new
                {
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstSolves", x => new { x.ParticipationId, x.ChallengeId });
                    table.ForeignKey(
                        name: "FK_FirstSolves_GameChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "GameChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FirstSolves_Participations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalTable: "Participations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FirstSolves_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirstSolves_ChallengeId",
                table: "FirstSolves",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_FirstSolves_SubmissionId",
                table: "FirstSolves",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.Sql(@"WITH first_submissions AS (
                SELECT DISTINCT ON (""ParticipationId"", ""ChallengeId"")
                    ""ParticipationId"",
                    ""ChallengeId"",
                    ""Id"" AS ""SubmissionId""
                FROM ""Submissions""
                WHERE ""Status"" = 'Accepted'
                ORDER BY ""ParticipationId"", ""ChallengeId"", ""SubmitTimeUtc"", ""Id""
            )
            INSERT INTO ""FirstSolves"" (""ParticipationId"", ""ChallengeId"", ""SubmissionId"")
            SELECT ""ParticipationId"", ""ChallengeId"", ""SubmissionId""
            FROM first_submissions;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirstSolves");

            migrationBuilder.AddColumn<bool>(
                name: "IsSolved",
                table: "GameInstances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "GameInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AcceptedCount",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSolved",
                table: "ExerciseInstances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "ExerciseInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AcceptedCount",
                table: "ExerciseChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "ExerciseChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
