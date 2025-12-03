using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContainerExposePort",
                table: "GameChallenges",
                newName: "ExposePort");

            migrationBuilder.RenameColumn(
                name: "ContainerExposePort",
                table: "ExerciseChallenges",
                newName: "ExposePort");

            migrationBuilder.AddColumn<byte>(
                name: "NetworkMode",
                table: "GameChallenges",
                type: "smallint",
                nullable: true,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "NetworkMode",
                table: "ExerciseChallenges",
                type: "smallint",
                nullable: true,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetworkMode",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "NetworkMode",
                table: "ExerciseChallenges");

            migrationBuilder.RenameColumn(
                name: "ExposePort",
                table: "GameChallenges",
                newName: "ContainerExposePort");

            migrationBuilder.RenameColumn(
                name: "ExposePort",
                table: "ExerciseChallenges",
                newName: "ContainerExposePort");
        }
    }
}
