using System;
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
            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "GameInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "GameInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionLimit",
                table: "GameChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "ExerciseInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "ExerciseInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionLimit",
                table: "ExerciseChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "GameInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionLimit",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "ExerciseInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "ExerciseInstances");

            migrationBuilder.DropColumn(
                name: "SubmissionLimit",
                table: "ExerciseChallenges");
        }
    }
}
