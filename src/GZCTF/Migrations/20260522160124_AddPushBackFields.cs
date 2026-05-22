using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddPushBackFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PushOnEdit",
                table: "GameRepoBindings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceYamlPath",
                table: "GameChallenges",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceYamlPath",
                table: "ExerciseChallenges",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PushOnEdit",
                table: "GameRepoBindings");

            migrationBuilder.DropColumn(
                name: "SourceYamlPath",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "SourceYamlPath",
                table: "ExerciseChallenges");
        }
    }
}
