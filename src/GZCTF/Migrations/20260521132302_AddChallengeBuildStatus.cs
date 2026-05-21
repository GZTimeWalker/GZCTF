using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeBuildStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuildImageDigest",
                table: "GameChallenges",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "BuildStatus",
                table: "GameChallenges",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "LastBuildLog",
                table: "GameChallenges",
                type: "character varying(32768)",
                maxLength: 32768,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildImageDigest",
                table: "ExerciseChallenges",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "BuildStatus",
                table: "ExerciseChallenges",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "LastBuildLog",
                table: "ExerciseChallenges",
                type: "character varying(32768)",
                maxLength: 32768,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildImageDigest",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "BuildStatus",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "LastBuildLog",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "BuildImageDigest",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "BuildStatus",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "LastBuildLog",
                table: "ExerciseChallenges");
        }
    }
}
