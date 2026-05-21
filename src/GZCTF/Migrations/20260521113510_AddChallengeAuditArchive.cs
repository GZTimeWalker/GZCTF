using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeAuditArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalArchiveBlobPath",
                table: "GameChallenges",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalArchiveBlobPath",
                table: "ExerciseChallenges",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalArchiveBlobPath",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "OriginalArchiveBlobPath",
                table: "ExerciseChallenges");
        }
    }
}
