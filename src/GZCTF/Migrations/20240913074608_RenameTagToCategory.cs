using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class RenameTagToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "GameChallenges",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "ExerciseChallenges",
                newName: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "GameChallenges",
                newName: "Tag");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "ExerciseChallenges",
                newName: "Tag");
        }
    }
}
