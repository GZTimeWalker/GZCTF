using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260418143000_AddPreStopCommand")]
    public partial class AddPreStopCommand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreStopCommand",
                table: "GameChallenges",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreStopCommand",
                table: "ExerciseChallenges",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreStopCommand",
                table: "Containers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreStopCommand",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "PreStopCommand",
                table: "ExerciseChallenges");

            migrationBuilder.DropColumn(
                name: "PreStopCommand",
                table: "Containers");
        }
    }
}
