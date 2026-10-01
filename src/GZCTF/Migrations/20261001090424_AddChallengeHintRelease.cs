using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeHintRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool[]>(
                name: "HintEnabled",
                table: "GameChallenges",
                type: "boolean[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HintEnabled",
                table: "GameChallenges");
        }
    }
}
