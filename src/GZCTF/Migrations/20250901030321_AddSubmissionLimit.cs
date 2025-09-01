using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubmissionLimit",
                table: "GameChallenges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionLimit",
                table: "ExerciseChallenges",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionLimit",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "SubmissionLimit",
                table: "ExerciseChallenges");
        }
    }
}
